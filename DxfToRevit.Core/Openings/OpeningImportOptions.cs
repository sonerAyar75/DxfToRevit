using DxfToRevit.Core.Import;

namespace DxfToRevit.Core.Openings;

/// <summary>How doors and windows are imported; set by the host in <see cref="ImportPlanContext.Options"/>.</summary>
/// <param name="TypeMap">DXF <c>TYPE</c> code → Revit type.</param>
public sealed record OpeningImportOptions(OpeningTypeMap TypeMap);
