#if REVIT_API_AVAILABLE
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.DB;
using DxfToRevit.Core.Columns;
using DxfToRevit.Revit.Common;

namespace DxfToRevit.Revit.Columns;

/// <summary>
/// Creates columns, in one of these ways:
/// <list type="bullet">
/// <item><description>Rectangular / circular: a column family instance bound to the base level and
/// either a top level or an unconnected height, rotated to the DXF orientation.</description></item>
/// <item><description>Circular with <see cref="ColumnTypeKeys.RoundDirectShape"/>, or when no round
/// family is available in automatic mode: a cylinder (<see cref="RevitColumnSolidWriter"/>).</description></item>
/// <item><description>Freeform with <see cref="ColumnTypeKeys.FreeformFamily"/>: a generated column
/// family instance bound to the levels; otherwise, or when the family cannot be built, an extruded
/// DirectShape.</description></item>
/// </list>
/// </summary>
internal sealed class RevitColumnWriter
{
    private readonly Document _document;
    private readonly RevitElementCreatePlanner _planner;
    private readonly RevitLevelResolver _levels;
    private readonly RevitColumnTypeResolver _types;
    private readonly RevitColumnSolidWriter _solids;
    private RevitFreeformColumnFamilyBuilder? _freeformFamilies;

    /// <summary>Creates the writer.</summary>
    /// <param name="document">Project document.</param>
    /// <param name="planner">Converts requests to create arguments in feet.</param>
    /// <param name="levels">Shared level resolver.</param>
    /// <param name="types">Column type resolver.</param>
    /// <param name="solids">Writer of columns without a family.</param>
    public RevitColumnWriter(
        Document document,
        RevitElementCreatePlanner planner,
        RevitLevelResolver levels,
        RevitColumnTypeResolver types,
        RevitColumnSolidWriter solids)
    {
        _document = document;
        _planner = planner;
        _levels = levels;
        _types = types;
        _solids = solids;
    }

    /// <summary>Creates the column of a request.</summary>
    /// <param name="request">Column request.</param>
    public ColumnCreationResult Create(ColumnCreationRequest request)
    {
        ColumnCreateArgs args;
        try { args = _planner.PlanColumn(request); }
        catch (ArgumentException ex) { return ColumnCreationResult.Fail(request.Id, ex.Message); }

        if (args.SectionKind == ColumnSectionKind.Freeform)
            return CreateFreeform(args);

        if (args.SectionKind == ColumnSectionKind.Circular &&
            string.Equals(args.FamilyTypeKey, ColumnTypeKeys.RoundDirectShape, StringComparison.Ordinal))
            return _solids.CreateRound(args, note: null);

        double? widthOrDia = args.SectionKind == ColumnSectionKind.Circular
            ? args.DiameterMm ?? args.WidthMm
            : args.WidthMm;
        double? depthOrDia = args.SectionKind == ColumnSectionKind.Circular
            ? widthOrDia
            : args.DepthMm;
        if (widthOrDia is not double widthMm || depthOrDia is not double depthMm)
            return ColumnCreationResult.Fail(request.Id, "Column width/depth (or diameter) is missing.");

        try
        {
            if (!_levels.TryResolve(args.Placement.LevelName, args.Placement.TopLevelName,
                    out Level level, out Level? topLevel, out string? levelError))
                return ColumnCreationResult.Fail(request.Id, levelError!);

            FamilySymbol? symbol = _types.Resolve(
                args.FamilyTypeKey, args.SectionKind, widthMm, depthMm, out string? typeError);
            if (symbol is null)
            {
                // In automatic round mode, create a cylinder instead of losing the column.
                if (args.SectionKind == ColumnSectionKind.Circular &&
                    string.Equals(args.FamilyTypeKey, ColumnTypeKeys.AutoRoundFamily, StringComparison.Ordinal))
                {
                    return _solids.CreateRound(args,
                        note: "No round column family available; created as a round solid (DirectShape).");
                }

                return ColumnCreationResult.Fail(request.Id, typeError ?? "Column type not resolved.");
            }

            return PlaceFamilyColumn(args, symbol, level, topLevel, args.RotationRadians, note: null);
        }
        catch (Exception ex)
        {
            return ColumnCreationResult.Fail(request.Id, ex.Message);
        }
    }

    /// <summary>
    /// Places a column family instance: base level and offset, top level or height, then the plan
    /// rotation. Shared by rectangular, circular and generated freeform family columns.
    /// </summary>
    private ColumnCreationResult PlaceFamilyColumn(
        ColumnCreateArgs args, FamilySymbol symbol, Level level, Level? topLevel, double rotationRadians, string? note)
    {
        var structuralType = args.IsStructural ? StructuralType.Column : StructuralType.NonStructural;
        var location = new XYZ(args.LocationXFeet, args.LocationYFeet, level.Elevation);

        using var tx = RevitChangeScope.Start(_document, "DxfToRevit Create Column");

        if (!symbol.IsActive)
            symbol.Activate();

        FamilyInstance column = _document.Create.NewFamilyInstance(location, symbol, level, structuralType);

        if (!RevitColumnPlacement.TryApplyHeight(
                column, level, args.Placement.BaseOffsetFeet, args.Placement.HeightFeet, out string? heightError, topLevel))
        {
            tx.RollBack();
            return ColumnCreationResult.Fail(args.RequestId, heightError ?? "Could not set column height.");
        }

        Rotate(column, location, rotationRadians);
        tx.Commit();

        return new ColumnCreationResult(args.RequestId, true, column.Id.Value.ToString(), note);
    }

    /// <summary>
    /// Creates a freeform column. With <see cref="ColumnTypeKeys.FreeformFamily"/> a column family is
    /// generated from the section; if that fails, or for any other key, an extruded DirectShape is
    /// created and the reason for the fallback is returned as a note.
    /// </summary>
    private ColumnCreationResult CreateFreeform(ColumnCreateArgs args)
    {
        if (args.OutlineFeet.Count < 3)
            return ColumnCreationResult.Fail(args.RequestId, "Freeform column outline needs at least 3 vertices.");

        string? familyNote = null;
        if (string.Equals(args.FamilyTypeKey, ColumnTypeKeys.FreeformFamily, StringComparison.Ordinal))
        {
            ColumnCreationResult? familyResult = TryCreateFreeformFamilyColumn(args, out string? familyError);
            if (familyResult is not null)
                return familyResult;

            // The family could not be built: keep the column as a DirectShape and note why.
            familyNote = $"Freeform column family could not be created ({familyError}); created as DirectShape.";
        }

        return _solids.CreateFreeform(args, familyNote);
    }

    /// <summary>
    /// Builds (or reuses) a column family for the freeform section and places it. Returns
    /// <c>null</c> with <paramref name="error"/> set when the family path fails, so the caller can fall
    /// back to a DirectShape. Level errors return a failed result directly, because a DirectShape
    /// would fail the same way.
    /// </summary>
    private ColumnCreationResult? TryCreateFreeformFamilyColumn(ColumnCreateArgs args, out string? error)
    {
        error = null;
        try
        {
            if (!_levels.TryResolve(args.Placement.LevelName, args.Placement.TopLevelName,
                    out Level level, out Level? topLevel, out string? levelError))
                return ColumnCreationResult.Fail(args.RequestId, levelError!);

            // The family origin is the column placement point (section centroid); the profile is relative to it.
            var relativeProfile = args.OutlineFeet
                .Select(p => (p.X - args.LocationXFeet, p.Y - args.LocationYFeet))
                .ToList();

            _freeformFamilies ??= new RevitFreeformColumnFamilyBuilder(_document);
            FamilySymbol? symbol = _freeformFamilies.GetOrCreate(relativeProfile, out error);
            if (symbol is null)
                return null;

            ColumnCreationResult placed = PlaceFamilyColumn(args, symbol, level, topLevel, 0, note: null);
            if (placed.Success)
                return placed;

            error = placed.Message;
            return null;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return null;
        }
    }

    /// <summary>
    /// Rotates the column about the vertical axis through its placement point (no-op for angle 0).
    /// The family width runs along local X, so after rotation it follows the DXF side directions.
    /// </summary>
    private void Rotate(FamilyInstance column, XYZ location, double angleRadians)
    {
        if (Math.Abs(angleRadians) < 1e-9)
            return;

        var axis = Line.CreateBound(location, location + XYZ.BasisZ);
        ElementTransformUtils.RotateElement(_document, column.Id, axis, angleRadians);
    }
}
#endif
