#if REVIT_API_AVAILABLE
using Autodesk.Revit.DB;
using DxfToRevit.Core.Walls;
using DxfToRevit.Revit.Common;

namespace DxfToRevit.Revit.Walls;

/// <summary>
/// Creates a Basic Wall on the base level, with the unconnected height or bound to the top level,
/// in its own (sub-)transaction.
/// </summary>
internal sealed class RevitWallWriter
{
    private readonly Document _document;
    private readonly RevitElementCreatePlanner _planner;
    private readonly RevitLevelResolver _levels;
    private readonly RevitWallTypeResolver _types;

    /// <summary>Creates the writer.</summary>
    /// <param name="document">Project document.</param>
    /// <param name="planner">Converts requests to create arguments in feet.</param>
    /// <param name="levels">Shared level resolver.</param>
    /// <param name="types">Wall type resolver.</param>
    public RevitWallWriter(
        Document document, RevitElementCreatePlanner planner, RevitLevelResolver levels, RevitWallTypeResolver types)
    {
        _document = document;
        _planner = planner;
        _levels = levels;
        _types = types;
    }

    /// <summary>Creates the wall of a request.</summary>
    /// <param name="request">Wall request.</param>
    public WallCreationResult Create(WallCreationRequest request)
    {
        WallCreateArgs args;
        try { args = _planner.PlanWall(request); }
        catch (ArgumentException ex) { return WallCreationResult.Fail(request.Id, ex.Message); }

        try
        {
            var line = Line.CreateBound(
                new XYZ(args.StartXFeet, args.StartYFeet, 0),
                new XYZ(args.EndXFeet, args.EndYFeet, 0));

            if (!_levels.TryResolve(args.LevelName, args.TopLevelName, out Level level, out Level? topLevel, out string? levelError))
                return WallCreationResult.Fail(request.Id, levelError!);

            WallType? wallType = _types.Resolve(args.WallTypeKey, args.ThicknessMm, out string? typeError);
            if (wallType is null)
                return WallCreationResult.Fail(request.Id, typeError ?? "WallType not resolved.");

            using var tx = RevitChangeScope.Start(_document, "DxfToRevit Create Wall");

            Wall wall = Wall.Create(
                _document, line, wallType.Id, level.Id,
                args.HeightFeet, args.BaseOffsetFeet, false, args.IsStructural);

            if (topLevel is not null)
            {
                Parameter? topConstraint = wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE);
                if (topConstraint is null || topConstraint.IsReadOnly)
                    throw new InvalidOperationException("Wall Top Constraint parameter is unavailable or read-only.");

                if (!topConstraint.Set(topLevel.Id))
                    throw new InvalidOperationException("Could not set wall Top Constraint.");
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
}
#endif
