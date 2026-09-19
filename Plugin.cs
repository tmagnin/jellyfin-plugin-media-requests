using System;
using System.Collections.Generic;
using Jellyfin.Plugin.MediaRequests.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.MediaRequests;

public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    public static Plugin? Instance { get; private set; }

    public override string Name => "Media Requests";

    public override Guid Id => Guid.Parse("f3a8c2d1-7b4e-4c19-9d5a-2e6b8a1c0f47");

    public override string Description =>
        "Lets users search for movies and shows and request them for the admin to add.";

    public IEnumerable<PluginPageInfo> GetPages()
    {
        var ns = GetType().Namespace;

        // Admin settings (Dashboard > Plugins > Media Requests).
        yield return new PluginPageInfo
        {
            Name = "MediaRequestsConfig",
            EmbeddedResourcePath = $"{ns}.Configuration.configPage.html"
        };

        // The user-facing page. EnableInMainMenu puts a link in the sidebar for everyone.
        yield return new PluginPageInfo
        {
            Name = "MediaRequests",
            DisplayName = "Requests",
            EmbeddedResourcePath = $"{ns}.Web.requests.html",
            EnableInMainMenu = true,
            MenuIcon = "playlist_add"
        };
    }
}
