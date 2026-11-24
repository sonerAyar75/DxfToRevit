using DxfToRevit.Core.Columns;
using DxfToRevit.Core.Import;
using DxfToRevit.Core.Openings;

namespace DxfToRevit.Tests.Import;

/// <summary>The session settings of the user and the module options made from them.</summary>
public sealed class ImportSettingsTests
{
    [Fact]
    public void ColumnSettings_Defaults_AutomaticRoundFamilyAndFreeformFamilies()
    {
        ColumnImportOptions options = new ColumnImportSettings().ToOptions();

        Assert.Equal(ColumnTypeKeys.AutoRoundFamily, options.CircularTypeKey);
        Assert.False(options.ConvertCircularToSquare);
        Assert.Equal(ColumnTypeKeys.FreeformFamily, options.FreeformTypeKey);
        Assert.True(options.IncludeCircular);
        Assert.True(options.IncludeFreeform);
    }

    [Fact]
    public void ColumnSettings_ChosenCircularType_IsUsed()
    {
        var settings = new ColumnImportSettings { CircularTypeKey = "M_Round Column::450 mm" };

        Assert.Equal("M_Round Column::450 mm", settings.ToOptions().CircularTypeKey);
    }

    [Fact]
    public void ColumnSettings_RoundSolid_UsesTheDirectShapeKeyWhateverTypeIsChosen()
    {
        var settings = new ColumnImportSettings
        {
            CircularMode = CircularColumnMode.RoundSolid,
            CircularTypeKey = "M_Round Column::450 mm"
        };

        Assert.Equal(ColumnTypeKeys.RoundDirectShape, settings.ToOptions().CircularTypeKey);
    }

    [Fact]
    public void ColumnSettings_SquareFallback_ConvertsWithTheSquareType()
    {
        var settings = new ColumnImportSettings
        {
            CircularMode = CircularColumnMode.SquareFallback,
            SquareTypeKey = "M_Rectangular Column::400 x 400mm"
        };

        ColumnImportOptions options = settings.ToOptions();

        Assert.True(options.ConvertCircularToSquare);
        Assert.Equal("M_Rectangular Column::400 x 400mm", options.SquareTypeKey);
        Assert.True(options.IncludeCircular);
    }

    [Fact]
    public void ColumnSettings_SkipAndSolids()
    {
        var settings = new ColumnImportSettings
        {
            CircularMode = CircularColumnMode.Skip,
            IncludeFreeform = false,
            FreeformAsFamily = false
        };

        ColumnImportOptions options = settings.ToOptions();

        Assert.False(options.IncludeCircular);
        Assert.False(options.IncludeFreeform);
        Assert.Equal(ColumnImportSettings.FreeformSolidTypeKey, options.FreeformTypeKey);
    }

    [Fact]
    public void OpeningSettings_RegistryUsesTheDefaultHeights()
    {
        var settings = new OpeningImportSettings { DoorHeightCm = 220, WindowSillCm = 100 };

        OpeningDefinitionRegistry registry = settings.CreateRegistry();

        Assert.Equal(220, registry.Find("door", null)!.DefaultHeightCm);
        Assert.Equal(100, registry.Find("window", null)!.DefaultSillHeightCm);
        Assert.Equal((220, settings.WindowHeightCm, 100, settings.EmptyOpeningHeightCm), settings.DefaultHeights);
    }

    [Fact]
    public void OpeningSettings_OptionsCarryTheTypeMap()
    {
        var map = new OpeningTypeMap(new[] { new OpeningTypeMapEntry(OpeningCategory.Door, "1", "F::T") });
        var settings = new OpeningImportSettings { TypeMap = map };

        Assert.Same(map, settings.ToOptions().TypeMap);
    }

    [Fact]
    public void SelectionSettings_NewCategoriesAreIncludedByDefault()
    {
        var selection = new ImportSelectionSettings();
        var floors = new ImportCategory("floors", "Floors");

        selection.SetIncluded(ImportCategories.Windows, false);

        Assert.False(selection.IsIncluded(ImportCategories.Windows));
        Assert.True(selection.IsIncluded(ImportCategories.Doors));
        Assert.True(selection.IsIncluded(floors));
        Assert.True(selection.SkipExisting);

        selection.SetIncluded(ImportCategories.Windows, true);
        Assert.True(selection.IsIncluded(ImportCategories.Windows));
    }
}
