using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Shoko.Abstractions.Plugin;
using Shoko.Abstractions.Plugin.Models;
using Shoko.Plugin.FilterHelper.Services;

namespace Shoko.Plugin.FilterHelper;

public class Plugin : IPlugin, IPluginServiceRegistration
{
    public Guid ID { get; } = new("a7f3c291-8b4e-4d6a-9c1d-2e5f7b8a0c3d");
    public string Name { get; } = "Filter Helper";
    public string Description { get; } = "Lightweight filter endpoints for client-side filtering.";

    public IReadOnlyList<PluginPage> GetPages() => [];

    /// <summary>
    /// Register the filter conversion service for DI.
    /// </summary>
    public static void RegisterServices(
        IServiceCollection serviceCollection,
        IApplicationPaths applicationPaths
    )
    {
        serviceCollection.AddSingleton<FilterConversionService>();
    }
}
