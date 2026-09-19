using Jellyfin.Plugin.MediaRequests.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.MediaRequests;

public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        serviceCollection.AddSingleton<RequestStore>();
        serviceCollection.AddSingleton<TmdbClient>();
        serviceCollection.AddSingleton<LibraryChecker>();

        // Adds the Requests button to the web client (see IndexInjectionMiddleware).
        serviceCollection.AddSingleton<IStartupFilter, IndexInjectionStartupFilter>();
    }
}
