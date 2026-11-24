#if REVIT_API_AVAILABLE
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB;
using DxfToRevit.Core.Openings;
using DxfToRevit.Revit.Common;
using DxfToRevit.Revit.Import;

namespace DxfToRevit.Revit.Openings;

/// <summary>
/// Creates doors and windows (family instances hosted by the wall) and empty wall openings, and
/// flips doors and windows to match the hinge circle of their marker. In a batch the flips are
/// deferred and applied together before the commit (<see cref="IRevitBatchParticipant"/>), so the
/// model regenerates twice in total instead of twice per instance.
/// </summary>
internal sealed class RevitOpeningWriter : IRevitBatchParticipant
{
    private readonly Document _document;
    private readonly OpeningCreateArgsMapper _mapper;
    private readonly RevitLevelResolver _levels;
    private readonly RevitOpeningTypeResolver _types;
    private readonly OpeningOrientationPlanner _orientation;
    private readonly List<PendingHanding> _pending = new();
    private bool _inBatch;

    /// <summary>Creates the writer.</summary>
    /// <param name="document">Project document.</param>
    /// <param name="mapper">Converts requests to create arguments in feet.</param>
    /// <param name="levels">Shared level resolver.</param>
    /// <param name="types">Door and window type resolver.</param>
    /// <param name="orientation">Door and window flip conventions.</param>
    public RevitOpeningWriter(
        Document document,
        OpeningCreateArgsMapper mapper,
        RevitLevelResolver levels,
        RevitOpeningTypeResolver types,
        OpeningOrientationPlanner orientation)
    {
        _document = document;
        _mapper = mapper;
        _levels = levels;
        _types = types;
        _orientation = orientation;
    }

    /// <summary>Creates the door, window or empty opening of a request in its host wall.</summary>
    /// <param name="request">Opening request.</param>
    /// <param name="hostWallElementId">Element id of the host wall.</param>
    public OpeningCreationResult Create(OpeningCreationRequest request, string hostWallElementId)
    {
        OpeningCreateArgs args;
        try { args = _mapper.Map(request); }
        catch (ArgumentException ex) { return OpeningCreationResult.Fail(request.Id, ex.Message); }

        try
        {
            if (!long.TryParse(hostWallElementId, out long wallIdValue) ||
                _document.GetElement(new ElementId(wallIdValue)) is not Wall wall)
                return OpeningCreationResult.Fail(request.Id, $"Host wall element {hostWallElementId} was not found.");

            Level? level = _levels.Resolve(args.LevelName, out string? levelError);
            if (level is null)
                return OpeningCreationResult.Fail(request.Id, levelError ?? "Level not resolved.");

            return args.CreationMode == OpeningCreationMode.WallOpening
                ? CreateWallOpening(args, wall, level)
                : CreateHostedFamily(args, wall, level);
        }
        catch (Exception ex)
        {
            return OpeningCreationResult.Fail(request.Id, ex.Message);
        }
    }

    /// <inheritdoc />
    public void OnBatchStarted()
    {
        _pending.Clear();
        _inBatch = true;
    }

    /// <inheritdoc />
    /// <remarks>Applies the door and window flips deferred during the batch.</remarks>
    public IReadOnlyDictionary<string, string> BeforeCommit()
    {
        IReadOnlyDictionary<string, string> notes = ApplyHandings(_pending);
        _pending.Clear();
        return notes;
    }

    /// <inheritdoc />
    public void OnBatchEnded()
    {
        _pending.Clear();
        _inBatch = false;
    }

    /// <summary>
    /// Places a door or window family instance in the wall, sets its sill height and flips it so the
    /// hinge and swing match the DXF marker.
    /// </summary>
    private OpeningCreationResult CreateHostedFamily(OpeningCreateArgs args, Wall wall, Level level)
    {
        if (string.IsNullOrWhiteSpace(args.TypeKey))
            return OpeningCreationResult.Fail(args.RequestId, $"No Revit type is mapped for this {args.Category}.");

        FamilySymbol? symbol = _types.Resolve(args.TypeKey, args.Category, args.WidthMm, args.HeightMm, out string? typeError);
        if (symbol is null)
            return OpeningCreationResult.Fail(args.RequestId, typeError ?? $"{args.Category} type not resolved.");

        var location = new XYZ(args.LocationXFeet, args.LocationYFeet, level.Elevation);

        using var tx = RevitChangeScope.Start(_document, $"DxfToRevit Create {args.Category}");

        if (!symbol.IsActive)
            symbol.Activate();

        FamilyInstance instance = _document.Create.NewFamilyInstance(
            location, symbol, wall, level, StructuralType.NonStructural);

        string? note = null;
        if (args.Category == OpeningCategory.Window || args.SillHeightFeet > 1e-9)
        {
            Parameter? sill = instance.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM);
            if (sill is { IsReadOnly: false })
                sill.Set(args.SillHeightFeet);
            else
                note = "Sill height parameter is not writable; the family default was kept.";
        }

        var pending = new PendingHanding(instance.Id, args);
        if (args.Handing is not null)
        {
            if (_inBatch)
            {
                // In a batch all doors and windows are flipped together, with two regenerations in total.
                _pending.Add(pending);
            }
            else
            {
                note = Combine(note, ApplyHandings(new[] { pending }).GetValueOrDefault(args.RequestId));
            }
        }

        tx.Commit();
        return new OpeningCreationResult(args.RequestId, true, instance.Id.Value.ToString(), note);
    }

    /// <summary>
    /// Flips each instance's facing, then its hand, when its orientation differs from the DXF
    /// marker. The orientations are read after one regeneration per step, not per instance. Returns a
    /// note (by request id) when a required flip is not allowed by the family.
    /// </summary>
    private IReadOnlyDictionary<string, string> ApplyHandings(IReadOnlyCollection<PendingHanding> items)
    {
        var notes = new Dictionary<string, string>(StringComparer.Ordinal);
        if (items.Count == 0)
            return notes;

        var placed = items
            .Select(p => (p.Args, Instance: _document.GetElement(p.InstanceId) as FamilyInstance))
            .Where(d => d.Instance is { IsValidObject: true } && d.Args.Handing is not null)
            .ToList();

        _document.Regenerate();
        bool facingFlipped = false;
        foreach (var (args, instance) in placed)
        {
            XYZ facing = instance!.FacingOrientation;
            if (!_orientation.NeedsFacingFlip(args.Category, (args.DirectionX, args.DirectionY), args.Handing!, (facing.X, facing.Y)))
                continue;

            if (instance.CanFlipFacing)
            {
                instance.flipFacing();
                facingFlipped = true;
            }
            else
            {
                notes[args.RequestId] = "The family cannot flip facing; the swing side may differ from the DXF.";
            }
        }

        if (facingFlipped)
            _document.Regenerate();

        foreach (var (args, instance) in placed)
        {
            XYZ hand = instance!.HandOrientation;
            if (!_orientation.NeedsHandFlip(args.Category, (args.DirectionX, args.DirectionY), args.Handing!, (hand.X, hand.Y)))
                continue;

            if (instance.CanFlipHand)
                instance.flipHand();
            else
                notes[args.RequestId] = Combine(notes.GetValueOrDefault(args.RequestId),
                    "The family cannot flip hand; the hinge side may differ from the DXF.")!;
        }

        return notes;
    }

    /// <summary>Cuts a rectangular opening in the wall from the sill height up to sill + height.</summary>
    private OpeningCreationResult CreateWallOpening(OpeningCreateArgs args, Wall wall, Level level)
    {
        double half = args.WidthFeet / 2.0;
        double bottom = level.Elevation + args.SillHeightFeet;
        var first = new XYZ(
            args.LocationXFeet - args.DirectionX * half, args.LocationYFeet - args.DirectionY * half, bottom);
        var second = new XYZ(
            args.LocationXFeet + args.DirectionX * half, args.LocationYFeet + args.DirectionY * half, bottom + args.HeightFeet);

        using var tx = RevitChangeScope.Start(_document, "DxfToRevit Create Wall Opening");
        Opening opening = _document.Create.NewOpening(wall, first, second);
        tx.Commit();

        return OpeningCreationResult.Ok(args.RequestId, opening.Id.Value.ToString());
    }

    /// <summary>Joins two optional notes with a space.</summary>
    private static string? Combine(string? first, string? second) =>
        first is null ? second : second is null ? first : $"{first} {second}";

    /// <summary>A placed door or window whose hinge and swing are applied later (in a batch).</summary>
    private sealed record PendingHanding(ElementId InstanceId, OpeningCreateArgs Args);
}
#endif
