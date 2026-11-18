using DxfToRevit.Core.Building;

namespace DxfToRevit.Revit;

/// <summary>
/// <see cref="IRevitDocumentContext"/> bound to a real Revit document. Requests are first converted
/// to create arguments by <see cref="RevitElementCreatePlanner"/>; levels and types are resolved by
/// <c>RevitElementResolver</c>; each element is then created in its own transaction.
/// </summary>
/// <remarks>
/// <para>Columns are created in one of these ways:</para>
/// <list type="bullet">
/// <item><description>Rectangular / circular: an architectural column family instance, bound to the base
/// level and either a top level or an unconnected height, and rotated to the DXF orientation.</description></item>
/// <item><description>Circular with <see cref="ColumnTypeKeys.RoundDirectShape"/>, or when no round family is
/// available in automatic mode: a cylindrical DirectShape.</description></item>
/// <item><description>Freeform with <see cref="ColumnTypeKeys.FreeformFamily"/>: a generated column family
/// instance (bound to levels); otherwise, or when the family cannot be built, an extruded DirectShape.</description></item>
/// </list>
/// <para>
/// Without the <c>REVIT_API_AVAILABLE</c> symbol the class compiles as a stub that is never available
/// and reports the planned values in its failure messages.
/// </para>
/// </remarks>
public sealed class RevitApiDocumentContext : IRevitDocumentContext
{
    private readonly RevitElementCreatePlanner _planner = new();

#if REVIT_API_AVAILABLE
    private readonly Autodesk.Revit.DB.Document _document;
    private readonly RevitElementResolver _resolver;
    private RevitFreeformColumnFamilyBuilder? _freeformFamilies;

    /// <summary>Binds the context to a project document.</summary>
    /// <param name="document">Open Revit project document.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public RevitApiDocumentContext(Autodesk.Revit.DB.Document document)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _resolver = new RevitElementResolver(document);
    }

    /// <inheritdoc />
    public bool IsAvailable => _document is { IsValidObject: true } && !_document.IsReadOnly;

    /// <inheritdoc />
    public WallCreationResult CreateBasicWall(WallCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!IsAvailable)
            return WallCreationResult.Fail(request.Id, "Revit Document is not available or is read-only.");

        WallCreateArgs args;
        try { args = _planner.PlanWall(request); }
        catch (ArgumentException ex) { return WallCreationResult.Fail(request.Id, ex.Message); }

        try
        {
            var line = Autodesk.Revit.DB.Line.CreateBound(
                new Autodesk.Revit.DB.XYZ(args.StartXFeet, args.StartYFeet, 0),
                new Autodesk.Revit.DB.XYZ(args.EndXFeet, args.EndYFeet, 0));

            Autodesk.Revit.DB.Level? level = _resolver.ResolveLevel(args.LevelName, out string? levelError);
            if (level is null)
                return WallCreationResult.Fail(request.Id, levelError ?? "Level not resolved.");

            if (!TryResolveTopLevel(args.TopLevelName, level, out Autodesk.Revit.DB.Level? topLevel, out string? topLevelError))
                return WallCreationResult.Fail(request.Id, topLevelError!);

            Autodesk.Revit.DB.WallType? wallType =
                _resolver.ResolveWallType(args.WallTypeKey, args.ThicknessMm, out string? typeError);
            if (wallType is null)
                return WallCreationResult.Fail(request.Id, typeError ?? "WallType not resolved.");

            using var tx = new Autodesk.Revit.DB.Transaction(_document, "DxfToRevit Create Wall");
            tx.Start();

            Autodesk.Revit.DB.Wall wall = Autodesk.Revit.DB.Wall.Create(
                _document, line, wallType.Id, level.Id,
                args.HeightFeet, args.BaseOffsetFeet, false, args.IsStructural);

            if (topLevel is not null)
            {
                var topConstraintParameter = wall.get_Parameter(
                    Autodesk.Revit.DB.BuiltInParameter.WALL_HEIGHT_TYPE);

                if (topConstraintParameter is null || topConstraintParameter.IsReadOnly)
                    throw new InvalidOperationException(
                        "Wall Top Constraint parameter is unavailable or read-only.");

                if (!topConstraintParameter.Set(topLevel.Id))
                    throw new InvalidOperationException(
                        "Could not set wall Top Constraint.");
            }

            tx.Commit();
            return WallCreationResult.Ok(request.Id, wall.Id.Value.ToString());
        }
        catch (Exception ex)
        {
            // An uncommitted transaction is rolled back when it is disposed.
            return WallCreationResult.Fail(request.Id, ex.Message);
        }
    }

    /// <inheritdoc />
    public ColumnCreationResult CreateColumn(ColumnCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!IsAvailable)
            return ColumnCreationResult.Fail(request.Id, "Revit Document is not available or is read-only.");

        ColumnCreateArgs args;
        try { args = _planner.PlanColumn(request); }
        catch (ArgumentException ex) { return ColumnCreationResult.Fail(request.Id, ex.Message); }

        if (args.SectionKind == ColumnSectionKind.Freeform)
            return CreateFreeformColumn(args);

        if (args.SectionKind == ColumnSectionKind.Circular &&
            string.Equals(args.FamilyTypeKey, ColumnTypeKeys.RoundDirectShape, StringComparison.Ordinal))
            return CreateRoundDirectShape(args, note: null);

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
            Autodesk.Revit.DB.Level? level = _resolver.ResolveLevel(args.Placement.LevelName, out string? levelError);
            if (level is null)
                return ColumnCreationResult.Fail(request.Id, levelError ?? "Level not resolved.");

            if (!TryResolveTopLevel(args.Placement.TopLevelName, level, out Autodesk.Revit.DB.Level? topLevel, out string? topLevelError))
                return ColumnCreationResult.Fail(request.Id, topLevelError!);

            Autodesk.Revit.DB.FamilySymbol? symbol = _resolver.ResolveColumnSymbol(
                args.FamilyTypeKey, args.SectionKind, widthMm, depthMm, out string? typeError);
            if (symbol is null)
            {
                // In automatic round mode, create a cylinder instead of losing the column.
                if (args.SectionKind == ColumnSectionKind.Circular &&
                    string.Equals(args.FamilyTypeKey, ColumnTypeKeys.AutoRoundFamily, StringComparison.Ordinal))
                {
                    return CreateRoundDirectShape(args,
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
        ColumnCreateArgs args,
        Autodesk.Revit.DB.FamilySymbol symbol,
        Autodesk.Revit.DB.Level level,
        Autodesk.Revit.DB.Level? topLevel,
        double rotationRadians,
        string? note)
    {
        var structuralType = args.IsStructural
            ? Autodesk.Revit.DB.Structure.StructuralType.Column
            : Autodesk.Revit.DB.Structure.StructuralType.NonStructural;

        var location = new Autodesk.Revit.DB.XYZ(args.LocationXFeet, args.LocationYFeet, level.Elevation);

        using var tx = new Autodesk.Revit.DB.Transaction(_document, "DxfToRevit Create Column");
        tx.Start();

        if (!symbol.IsActive)
            symbol.Activate();

        Autodesk.Revit.DB.FamilyInstance column = _document.Create.NewFamilyInstance(
            location, symbol, level, structuralType);

        if (!RevitColumnPlacement.TryApplyHeight(
                column, level, args.Placement.BaseOffsetFeet, args.Placement.HeightFeet, out string? heightError, topLevel))
        {
            tx.RollBack();
            return ColumnCreationResult.Fail(args.RequestId, heightError ?? "Could not set column height.");
        }

        RotateColumn(column, location, rotationRadians);
        tx.Commit();

        return new ColumnCreationResult(args.RequestId, true, column.Id.Value.ToString(), note);
    }

    /// <summary>
    /// Creates a freeform column. With <see cref="ColumnTypeKeys.FreeformFamily"/> a column family is
    /// generated from the section; if that fails, or for any other key, an extruded DirectShape is
    /// created and the reason for the fallback is returned as a note.
    /// </summary>
    private ColumnCreationResult CreateFreeformColumn(ColumnCreateArgs args)
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

        return CreateExtrudedDirectShape(
            args,
            transactionName: "DxfToRevit Create Freeform Column",
            label: "Freeform",
            note: familyNote,
            buildProfile: baseZ =>
            {
                var profile = new Autodesk.Revit.DB.CurveLoop();
                for (int i = 0; i < args.OutlineFeet.Count; i++)
                {
                    var current = args.OutlineFeet[i];
                    var next = args.OutlineFeet[(i + 1) % args.OutlineFeet.Count];
                    var start = new Autodesk.Revit.DB.XYZ(current.X, current.Y, baseZ);
                    var end = new Autodesk.Revit.DB.XYZ(next.X, next.Y, baseZ);

                    if (start.DistanceTo(end) < 1e-8)
                        throw new InvalidOperationException("Freeform column outline contains a zero-length edge.");

                    profile.Append(Autodesk.Revit.DB.Line.CreateBound(start, end));
                }

                return profile;
            });
    }

    /// <summary>
    /// Builds (or reuses) a column family for the freeform section and places it.
    /// Returns <c>null</c> with <paramref name="error"/> set when the family path fails, so the caller
    /// can fall back to a DirectShape. Level and top level errors return a failed result directly,
    /// because a DirectShape would fail the same way.
    /// </summary>
    private ColumnCreationResult? TryCreateFreeformFamilyColumn(ColumnCreateArgs args, out string? error)
    {
        error = null;
        try
        {
            Autodesk.Revit.DB.Level? level = _resolver.ResolveLevel(args.Placement.LevelName, out string? levelError);
            if (level is null)
                return ColumnCreationResult.Fail(args.RequestId, levelError ?? "Level not resolved.");

            if (!TryResolveTopLevel(args.Placement.TopLevelName, level, out Autodesk.Revit.DB.Level? topLevel, out string? topLevelError))
                return ColumnCreationResult.Fail(args.RequestId, topLevelError!);

            // The family origin is the column placement point (section centroid); the profile is relative to it.
            var relativeProfile = args.OutlineFeet
                .Select(p => (p.X - args.LocationXFeet, p.Y - args.LocationYFeet))
                .ToList();

            _freeformFamilies ??= new RevitFreeformColumnFamilyBuilder(_document);
            Autodesk.Revit.DB.FamilySymbol? symbol = _freeformFamilies.GetOrCreate(relativeProfile, out error);
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
    /// Creates a circular column as a true cylinder (DirectShape), without a family.
    /// The category is Columns when Revit allows it (so it appears in column schedules), otherwise Generic Model.
    /// </summary>
    private ColumnCreationResult CreateRoundDirectShape(ColumnCreateArgs args, string? note)
    {
        double? diameterMm = args.DiameterMm ?? args.WidthMm;
        if (diameterMm is not double dia || dia <= 0)
            return ColumnCreationResult.Fail(args.RequestId, "Circular column diameter is missing.");

        double radiusFeet = LengthUnitConverter.MmToFeet(dia / 2.0);

        return CreateExtrudedDirectShape(
            args,
            transactionName: "DxfToRevit Create Round Column",
            label: "Round",
            note: note,
            buildProfile: baseZ =>
            {
                var center = new Autodesk.Revit.DB.XYZ(args.LocationXFeet, args.LocationYFeet, baseZ);
                var profile = new Autodesk.Revit.DB.CurveLoop();
                // A full circle cannot be a single closed arc; it is built from two half arcs.
                profile.Append(Autodesk.Revit.DB.Arc.Create(
                    center, radiusFeet, 0, Math.PI, Autodesk.Revit.DB.XYZ.BasisX, Autodesk.Revit.DB.XYZ.BasisY));
                profile.Append(Autodesk.Revit.DB.Arc.Create(
                    center, radiusFeet, Math.PI, 2 * Math.PI, Autodesk.Revit.DB.XYZ.BasisX, Autodesk.Revit.DB.XYZ.BasisY));
                return profile;
            });
    }

    /// <summary>
    /// Shared DirectShape column creation: resolves the levels, computes the height and extrudes the
    /// profile upwards. With a top level the height runs from the base to that level; a DirectShape
    /// cannot be bound to levels, so the height is taken from the level elevations at creation time.
    /// The category is Columns when Revit allows it for DirectShapes, otherwise Generic Model.
    /// </summary>
    /// <param name="args">Column arguments.</param>
    /// <param name="transactionName">Name of the transaction shown in the undo list.</param>
    /// <param name="label">Column kind used in error messages.</param>
    /// <param name="note">Informational note returned with a successful result.</param>
    /// <param name="buildProfile">Builds the closed profile at the given base elevation (feet).</param>
    private ColumnCreationResult CreateExtrudedDirectShape(
        ColumnCreateArgs args,
        string transactionName,
        string label,
        string? note,
        Func<double, Autodesk.Revit.DB.CurveLoop> buildProfile)
    {
        if (args.Placement.HeightFeet <= 0)
            return ColumnCreationResult.Fail(args.RequestId, $"{label} column height must be greater than zero.");

        try
        {
            Autodesk.Revit.DB.Level? level = _resolver.ResolveLevel(
                args.Placement.LevelName, out string? levelError);
            if (level is null)
                return ColumnCreationResult.Fail(args.RequestId, levelError ?? "Level not resolved.");

            if (!TryResolveTopLevel(args.Placement.TopLevelName, level, out Autodesk.Revit.DB.Level? topLevel, out string? topLevelError))
                return ColumnCreationResult.Fail(args.RequestId, topLevelError!);

            double baseZ = level.Elevation + args.Placement.BaseOffsetFeet;
            double extrusionHeightFeet = topLevel is not null
                ? topLevel.Elevation - baseZ
                : args.Placement.HeightFeet;
            if (extrusionHeightFeet <= 1e-6)
                return ColumnCreationResult.Fail(args.RequestId, $"{label} column top must be above its base (check Top Level and Base Offset).");

            Autodesk.Revit.DB.CurveLoop profile;
            try { profile = buildProfile(baseZ); }
            catch (InvalidOperationException ex) { return ColumnCreationResult.Fail(args.RequestId, ex.Message); }

            Autodesk.Revit.DB.Solid solid = Autodesk.Revit.DB.GeometryCreationUtilities.CreateExtrusionGeometry(
                new List<Autodesk.Revit.DB.CurveLoop> { profile },
                Autodesk.Revit.DB.XYZ.BasisZ,
                extrusionHeightFeet);

            var categoryId = new Autodesk.Revit.DB.ElementId(Autodesk.Revit.DB.BuiltInCategory.OST_GenericModel);
            var columns = new Autodesk.Revit.DB.ElementId(Autodesk.Revit.DB.BuiltInCategory.OST_Columns);
            if (Autodesk.Revit.DB.DirectShape.IsValidCategoryId(columns, _document))
                categoryId = columns;

            using var tx = new Autodesk.Revit.DB.Transaction(_document, transactionName);
            tx.Start();
            Autodesk.Revit.DB.DirectShape directShape = CreateDirectShape(categoryId, args.RequestId, solid);
            tx.Commit();

            return new ColumnCreationResult(args.RequestId, true, directShape.Id.Value.ToString(), note);
        }
        catch (Exception ex)
        {
            return ColumnCreationResult.Fail(args.RequestId, $"{label} DirectShape creation failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Rotates the column about the vertical axis through its placement point (no-op for angle 0).
    /// The family width runs along local X, so after rotation it follows the DXF side directions.
    /// </summary>
    private void RotateColumn(Autodesk.Revit.DB.FamilyInstance column, Autodesk.Revit.DB.XYZ location, double angleRadians)
    {
        if (Math.Abs(angleRadians) < 1e-9)
            return;

        var axis = Autodesk.Revit.DB.Line.CreateBound(location, location + Autodesk.Revit.DB.XYZ.BasisZ);
        Autodesk.Revit.DB.ElementTransformUtils.RotateElement(_document, column.Id, axis, angleRadians);
    }

    /// <summary>Creates a DirectShape with the solid, tagged with the add-in id and the request id.</summary>
    private Autodesk.Revit.DB.DirectShape CreateDirectShape(
        Autodesk.Revit.DB.ElementId categoryId, string requestId, Autodesk.Revit.DB.Solid solid)
    {
        var directShape = Autodesk.Revit.DB.DirectShape.CreateElement(_document, categoryId);
        directShape.ApplicationId = "DxfToRevit";
        directShape.ApplicationDataId = requestId;
        directShape.SetShape(new List<Autodesk.Revit.DB.GeometryObject> { solid });
        return directShape;
    }

    /// <summary>
    /// Resolves the top level name. A blank name means "Unconnected" (returns <c>true</c> with
    /// <paramref name="topLevel"/> = <c>null</c>). Returns <c>false</c> when the level is not found or
    /// is not above the base level.
    /// </summary>
    private bool TryResolveTopLevel(
        string? topLevelName,
        Autodesk.Revit.DB.Level baseLevel,
        out Autodesk.Revit.DB.Level? topLevel,
        out string? error)
    {
        topLevel = null;
        error = null;

        if (string.IsNullOrWhiteSpace(topLevelName))
            return true;

        topLevel = _resolver.ResolveLevel(topLevelName, out string? resolveError);
        if (topLevel is null)
        {
            error = resolveError ?? "Top Level not resolved.";
            return false;
        }

        if (topLevel.Elevation <= baseLevel.Elevation)
        {
            error = "Top Level must be higher than Base Level.";
            topLevel = null;
            return false;
        }

        return true;
    }
#else
    /// <summary>Stub constructor used when the Revit API is not available; the context is never available.</summary>
    /// <param name="document">Ignored.</param>
    public RevitApiDocumentContext(object? document = null) { _ = document; }

    /// <inheritdoc />
    public bool IsAvailable => false;

    /// <summary>Fails and reports the planned wall values (no Revit API in this build).</summary>
    /// <param name="request">Wall request.</param>
    public WallCreationResult CreateBasicWall(WallCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            WallCreateArgs args = _planner.PlanWall(request);
            return WallCreationResult.Fail(
                request.Id,
                "No REVIT_API_AVAILABLE. " +
                $"Planned wall: Type={args.WallTypeKey}, Level={args.LevelName ?? "(default)"}, " +
                $"H={args.HeightFeet:F4} ft, L={args.LengthFeet:F4} ft.");
        }
        catch (ArgumentException ex)
        {
            return WallCreationResult.Fail(request.Id, ex.Message);
        }
    }

    /// <summary>Fails and reports the planned column values (no Revit API in this build).</summary>
    /// <param name="request">Column request.</param>
    public ColumnCreationResult CreateColumn(ColumnCreationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        try
        {
            ColumnCreateArgs args = _planner.PlanColumn(request);
            return ColumnCreationResult.Fail(
                request.Id,
                "No REVIT_API_AVAILABLE. " +
                $"Planned column: Type={args.FamilyTypeKey}, Level={args.Placement.LevelName ?? "(default)"}, " +
                $"H={args.Placement.HeightFeet:F4} ft, W/D={args.WidthMm}/{args.DepthMm} mm.");
        }
        catch (ArgumentException ex)
        {
            return ColumnCreationResult.Fail(request.Id, ex.Message);
        }
    }
#endif
}
