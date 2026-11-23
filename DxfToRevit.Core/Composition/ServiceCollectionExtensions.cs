using DxfToRevit.Core.Building;
using DxfToRevit.Core.Building.InMemory;
using DxfToRevit.Core.Dxf;
using DxfToRevit.Core.Geometry;
using DxfToRevit.Core.IO;
using DxfToRevit.Core.Openings;
using DxfToRevit.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DxfToRevit.Core.Composition;

/// <summary>
/// Dependency injection registrations for the core library. The import policy is an
/// <see cref="ImportSessionOptions"/> singleton that the request factories consume.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the DXF readers, geometry services, the centerline pipeline, the simulation
    /// writer and the request factories/validators. A default <see cref="ImportSessionOptions"/>
    /// is registered as well; call <see cref="AddDxfToRevitImportSession"/> afterwards to override it.
    /// </summary>
    /// <param name="services">Service collection to extend.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <c>null</c>.</exception>
    public static IServiceCollection AddDxfToRevitCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(new ImportSessionOptions());
        services.AddTransient<DxfReader>();
        services.AddTransient<DxfGeometryReader>();
        services.AddTransient<DxfColumnReader>();
        services.AddTransient<DxfOpeningReader>();
        services.AddTransient<OpeningRegionFilter>();
        services.AddTransient<OpeningInterpreter>();
        services.AddTransient<UnmarkedGapReporter>();
        services.AddTransient<DxfLineClassifier>();
        services.AddTransient<DxfLineClassificationService>();
        services.AddTransient<DxfLineImportService>();
        services.AddTransient<WallFacePairFinder>();
        services.AddTransient<WallModelManager>();
        services.AddTransient<OpenSideCenterLineBridger>();
        services.AddTransient<WallCenterLineJunctionProcessor>();
        services.AddTransient<CollinearCenterLineMerger>(_ => new CollinearCenterLineMerger(maxGapMm: 5.0));
        services.AddTransient<WallFootprintBuilder>();
        services.AddTransient<DxfSimulationWriter>();
        services.AddTransient<DxfCenterLinePipeline>();
        services.AddTransient<WallCreationRequestFactory>();
        services.AddTransient<ColumnCreationRequestFactory>();
        services.AddTransient<WallCreationValidator>();
        services.AddTransient<ColumnCreationValidator>();
        services.AddTransient<BuildingExportFromDxf>();

        return services;
    }

    /// <summary>
    /// Registers the import session (levels, height, type mapping) used by the request factories.
    /// The last registration wins, so call this after <see cref="AddDxfToRevitCore"/>.
    /// </summary>
    /// <param name="services">Service collection to extend.</param>
    /// <param name="session">Session options to use.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException">An argument is <c>null</c>.</exception>
    public static IServiceCollection AddDxfToRevitImportSession(
        this IServiceCollection services,
        ImportSessionOptions session)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(session);
        services.AddSingleton(session);
        return services;
    }

    /// <summary>
    /// Registers the in-memory host (<see cref="InMemoryBuildingModel"/> and the in-memory wall and
    /// column creators) plus <see cref="BuildingModelService"/>. Useful for tests and simulations.
    /// </summary>
    /// <param name="services">Service collection to extend.</param>
    /// <returns>The same collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <c>null</c>.</exception>
    public static IServiceCollection AddDxfToRevitBuildingInMemory(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<InMemoryBuildingModel>();
        services.AddTransient<IWallCreator, InMemoryWallCreator>();
        services.AddTransient<IColumnCreator, InMemoryColumnCreator>();
        services.AddTransient<IOpeningCreator, InMemoryOpeningCreator>();
        services.AddTransient<BuildingModelService>();

        return services;
    }
}
