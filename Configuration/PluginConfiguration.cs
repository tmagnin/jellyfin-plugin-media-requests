using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.MediaRequests.Configuration;

public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>Gets or sets the TMDB v3 API key or v4 read access token.</summary>
    public string TmdbApiKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the language used for TMDB titles and summaries.</summary>
    public string TmdbLanguage { get; set; } = "en-US";

    /// <summary>Gets or sets how many pending requests one user may have at once. 0 means no limit.</summary>
    public int MaxPendingRequestsPerUser { get; set; } = 10;

    /// <summary>Gets or sets a value indicating whether adult titles appear in search results.</summary>
    public bool IncludeAdultResults { get; set; }
}
