# Testing {#testing}

## Test projects

| Project | Scope | Needs Revit |
|---------|-------|-------------|
| `DxfToRevit.Tests` | Geometry, DXF reading, models, rules, services, pipeline, building API (in-memory host) | No |
| `DxfToRevit.Revit.Tests` | Unit conversion, create-argument mapping, level/type planners, family search and naming, adapter smoke tests | No (optional) |

`DxfToRevit.Revit.Tests/Integration` contains tests that need an open Revit document; they are
marked `Skip` and serve as templates for an add-in-hosted test runner.

## Running

```bash
dotnet test DxfToRevit.Tests
dotnet test DxfToRevit.Revit.Tests

# Compile the adapter tests against the real Revit API as well
dotnet test DxfToRevit.Revit.Tests -p:RevitApiDir="C:\Program Files\Autodesk\Revit 2025"
```

Sample drawings used by the tests are in `Samples/`.

## What is covered

- Every geometry stage of the @ref pipeline, including inclined L/T junctions and openings.
- Column classification: circles, axis-aligned and rotated rectangles, freeform sections.
- Request factories and validators, the in-memory host and DI registrations.
- The Revit decisions that do not need the API: unit conversion, argument mapping, level and type
  selection, round family search order and freeform family naming.

The Revit API calls themselves (wall/column creation, family building) are verified manually in
Revit.
