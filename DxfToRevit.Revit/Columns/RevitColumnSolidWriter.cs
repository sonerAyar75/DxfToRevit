#if REVIT_API_AVAILABLE
using Autodesk.Revit.DB;
using DxfToRevit.Core.Columns;
using DxfToRevit.Revit.Columns;
using DxfToRevit.Revit.Common;
using DxfToRevit.Revit.Import;
using DxfToRevit.Revit.Openings;
using DxfToRevit.Revit.Walls;

namespace DxfToRevit.Revit.Columns;

/// <summary>
/// Creates columns without a family, as extruded DirectShapes: round columns as cylinders and
/// freeform columns from their outline. The category is Columns when Revit allows it for
/// DirectShapes (so they appear in column schedules), otherwise Generic Model. A DirectShape cannot
/// be bound to levels: with a top level its height is taken from the level elevations at creation.
/// </summary>
internal sealed class RevitColumnSolidWriter
{
    private readonly Document _document;
    private readonly RevitLevelResolver _levels;

    /// <summary>Creates the writer.</summary>
    /// <param name="document">Project document.</param>
    /// <param name="levels">Shared level resolver.</param>
    public RevitColumnSolidWriter(Document document, RevitLevelResolver levels)
    {
        _document = document;
        _levels = levels;
    }

    /// <summary>Creates a circular column as a cylinder.</summary>
    /// <param name="args">Column arguments.</param>
    /// <param name="note">Note returned with a successful result.</param>
    public ColumnCreationResult CreateRound(ColumnCreateArgs args, string? note)
    {
        double? diameterMm = args.DiameterMm ?? args.WidthMm;
        if (diameterMm is not double dia || dia <= 0)
            return ColumnCreationResult.Fail(args.RequestId, "Circular column diameter is missing.");

        double radiusFeet = LengthUnitConverter.MmToFeet(dia / 2.0);

        return CreateExtruded(
            args,
            transactionName: "DxfToRevit Create Round Column",
            label: "Round",
            note: note,
            buildProfile: baseZ =>
            {
                var center = new XYZ(args.LocationXFeet, args.LocationYFeet, baseZ);
                var profile = new CurveLoop();
                // A full circle cannot be a single closed arc; it is built from two half arcs.
                profile.Append(Arc.Create(center, radiusFeet, 0, Math.PI, XYZ.BasisX, XYZ.BasisY));
                profile.Append(Arc.Create(center, radiusFeet, Math.PI, 2 * Math.PI, XYZ.BasisX, XYZ.BasisY));
                return profile;
            });
    }

    /// <summary>Creates a freeform column by extruding its outline.</summary>
    /// <param name="args">Column arguments.</param>
    /// <param name="note">Note returned with a successful result (for example why no family was built).</param>
    public ColumnCreationResult CreateFreeform(ColumnCreateArgs args, string? note) =>
        CreateExtruded(
            args,
            transactionName: "DxfToRevit Create Freeform Column",
            label: "Freeform",
            note: note,
            buildProfile: baseZ =>
            {
                var profile = new CurveLoop();
                for (int i = 0; i < args.OutlineFeet.Count; i++)
                {
                    var current = args.OutlineFeet[i];
                    var next = args.OutlineFeet[(i + 1) % args.OutlineFeet.Count];
                    var start = new XYZ(current.X, current.Y, baseZ);
                    var end = new XYZ(next.X, next.Y, baseZ);

                    if (start.DistanceTo(end) < 1e-8)
                        throw new InvalidOperationException("Freeform column outline contains a zero-length edge.");

                    profile.Append(Line.CreateBound(start, end));
                }

                return profile;
            });

    /// <summary>
    /// Shared DirectShape creation: resolves the levels, computes the height and extrudes the profile
    /// upwards.
    /// </summary>
    /// <param name="args">Column arguments.</param>
    /// <param name="transactionName">Name of the transaction shown in the undo list.</param>
    /// <param name="label">Column kind used in error messages.</param>
    /// <param name="note">Informational note returned with a successful result.</param>
    /// <param name="buildProfile">Builds the closed profile at the given base elevation (feet).</param>
    private ColumnCreationResult CreateExtruded(
        ColumnCreateArgs args,
        string transactionName,
        string label,
        string? note,
        Func<double, CurveLoop> buildProfile)
    {
        if (args.Placement.HeightFeet <= 0)
            return ColumnCreationResult.Fail(args.RequestId, $"{label} column height must be greater than zero.");

        try
        {
            if (!_levels.TryResolve(args.Placement.LevelName, args.Placement.TopLevelName,
                    out Level level, out Level? topLevel, out string? levelError))
                return ColumnCreationResult.Fail(args.RequestId, levelError!);

            double baseZ = level.Elevation + args.Placement.BaseOffsetFeet;
            double extrusionHeightFeet = topLevel is not null
                ? topLevel.Elevation - baseZ
                : args.Placement.HeightFeet;
            if (extrusionHeightFeet <= 1e-6)
                return ColumnCreationResult.Fail(args.RequestId, $"{label} column top must be above its base (check Top Level and Base Offset).");

            CurveLoop profile;
            try { profile = buildProfile(baseZ); }
            catch (InvalidOperationException ex) { return ColumnCreationResult.Fail(args.RequestId, ex.Message); }

            Solid solid = GeometryCreationUtilities.CreateExtrusionGeometry(
                new List<CurveLoop> { profile }, XYZ.BasisZ, extrusionHeightFeet);

            var categoryId = new ElementId(BuiltInCategory.OST_GenericModel);
            var columns = new ElementId(BuiltInCategory.OST_Columns);
            if (DirectShape.IsValidCategoryId(columns, _document))
                categoryId = columns;

            using var tx = RevitChangeScope.Start(_document, transactionName);
            DirectShape directShape = CreateDirectShape(categoryId, args.RequestId, solid);
            tx.Commit();

            return new ColumnCreationResult(args.RequestId, true, directShape.Id.Value.ToString(), note);
        }
        catch (Exception ex)
        {
            return ColumnCreationResult.Fail(args.RequestId, $"{label} DirectShape creation failed: {ex.Message}");
        }
    }

    /// <summary>Creates a DirectShape with the solid, tagged with the add-in id and the request id.</summary>
    private DirectShape CreateDirectShape(ElementId categoryId, string requestId, Solid solid)
    {
        var directShape = DirectShape.CreateElement(_document, categoryId);
        directShape.ApplicationId = "DxfToRevit";
        directShape.ApplicationDataId = requestId;
        directShape.SetShape(new List<GeometryObject> { solid });
        return directShape;
    }
}
#endif
