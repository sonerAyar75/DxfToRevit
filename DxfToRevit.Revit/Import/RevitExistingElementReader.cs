#if REVIT_API_AVAILABLE
using Autodesk.Revit.DB;
using DxfToRevit.Core.Import;
using DxfToRevit.Core.Openings;
using DxfToRevit.Revit.Common;

namespace DxfToRevit.Revit.Import;

/// <summary>
/// Reads the straight walls, columns, doors, windows and wall openings of a document into an
/// API-free <see cref="ExistingElements"/> snapshot (millimetres), so that
/// <see cref="ExistingElementMatcher"/> can tell which import requests are already in the model.
/// </summary>
public static class RevitExistingElementReader
{
    /// <summary>Reads the snapshot.</summary>
    /// <param name="document">Project document.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="document"/> is <c>null</c>.</exception>
    public static ExistingElements Read(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        List<Level> levels = new FilteredElementCollector(document).OfClass(typeof(Level)).Cast<Level>()
            .OrderBy(l => l.Elevation).ToList();

        return new ExistingElements(ReadWalls(document), ReadColumns(document, levels), ReadOpenings(document));
    }

    private static List<ExistingWall> ReadWalls(Document document)
    {
        var walls = new List<ExistingWall>();
        foreach (Wall wall in new FilteredElementCollector(document).OfClass(typeof(Wall)).Cast<Wall>())
        {
            if (wall.Location is not LocationCurve { Curve: Line line })
                continue;

            walls.Add(new ExistingWall(
                Id(wall),
                LevelName(document, wall.LevelId),
                ToMm(line.GetEndPoint(0)),
                ToMm(line.GetEndPoint(1)),
                LengthUnitConverter.FeetToMm(wall.Width)));
        }

        return walls;
    }

    private static List<ExistingColumn> ReadColumns(Document document, IReadOnlyList<Level> levels)
    {
        var columns = new List<ExistingColumn>();
        foreach (BuiltInCategory category in new[] { BuiltInCategory.OST_Columns, BuiltInCategory.OST_StructuralColumns })
        {
            foreach (Element element in new FilteredElementCollector(document).OfCategory(category).WhereElementIsNotElementType())
            {
                switch (element)
                {
                    case FamilyInstance { Location: LocationPoint point } instance:
                        ElementId levelId = instance.LevelId;
                        if (levelId == ElementId.InvalidElementId)
                            levelId = instance.get_Parameter(BuiltInParameter.FAMILY_BASE_LEVEL_PARAM)?.AsElementId()
                                      ?? ElementId.InvalidElementId;
                        columns.Add(new ExistingColumn(Id(instance), LevelName(document, levelId), ToMm(point.Point)));
                        break;

                    case DirectShape shape when shape.get_BoundingBox(null) is BoundingBoxXYZ box:
                        // Column solids have no level: take the highest level at or below their bottom.
                        XYZ centre = (box.Min + box.Max) / 2;
                        string? level = levels.LastOrDefault(l => l.Elevation <= box.Min.Z + 1e-6)?.Name;
                        columns.Add(new ExistingColumn(Id(shape), level, ToMm(centre)));
                        break;
                }
            }
        }

        return columns;
    }

    private static List<ExistingOpening> ReadOpenings(Document document)
    {
        var openings = new List<ExistingOpening>();
        foreach ((BuiltInCategory builtIn, OpeningCategory category) in new[]
                 {
                     (BuiltInCategory.OST_Doors, OpeningCategory.Door),
                     (BuiltInCategory.OST_Windows, OpeningCategory.Window)
                 })
        {
            foreach (FamilyInstance instance in new FilteredElementCollector(document).OfCategory(builtIn)
                         .WhereElementIsNotElementType().OfType<FamilyInstance>())
            {
                if (instance.Location is LocationPoint point)
                    openings.Add(new ExistingOpening(Id(instance), category, LevelName(document, instance.LevelId), ToMm(point.Point)));
            }
        }

        foreach (Opening opening in new FilteredElementCollector(document).OfClass(typeof(Opening)).Cast<Opening>())
        {
            if (opening.Host is not Wall host || !opening.IsRectBoundary || opening.BoundaryRect is not { Count: 2 } rect)
                continue;

            XYZ centre = (rect[0] + rect[1]) / 2;
            openings.Add(new ExistingOpening(Id(opening), OpeningCategory.EmptyOpening, LevelName(document, host.LevelId), ToMm(centre)));
        }

        return openings;
    }

    private static string Id(Element element) => element.Id.Value.ToString();

    private static string? LevelName(Document document, ElementId levelId) =>
        levelId == ElementId.InvalidElementId ? null : (document.GetElement(levelId) as Level)?.Name;

    private static netDxf.Vector2 ToMm(XYZ point) =>
        new(LengthUnitConverter.FeetToMm(point.X), LengthUnitConverter.FeetToMm(point.Y));
}
#endif
