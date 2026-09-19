using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MediaRequests.Services;

public sealed record TmdbItem(
    int TmdbId,
    string MediaType,
    string Title,
    string? Year,
    string? Overview,
    string? PosterPath,
    double? Rating);

public sealed class TmdbException : Exception
{
    public TmdbException(string message, int statusCode)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public int StatusCode { get; }
}

/// <summary>Small TMDB client. The API key stays on the server; browsers only talk to Jellyfin.</summary>
public sealed class TmdbClient
{
    private const string ApiBase = "https://api.themoviedb.org/3";
    private const string ImageBase = "https://image.tmdb.org/t/p/";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TmdbClient> _logger;

    public TmdbClient(IHttpClientFactory httpClientFactory, ILogger<TmdbClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public static string? PosterUrl(string? posterPath, string size = "w342") =>
        string.IsNullOrEmpty(posterPath) ? null : $"{ImageBase}{size}{posterPath}";

    public async Task<IReadOnlyList<TmdbItem>> SearchAsync(string query, string mediaType, CancellationToken ct)
    {
        var cfg = Plugin.Instance?.Configuration;
        var endpoint = mediaType switch
        {
            "movie" => "search/movie",
            "tv" => "search/tv",
            _ => "search/multi"
        };

        var path = $"{endpoint}?query={Uri.EscapeDataString(query)}" +
                   $"&include_adult={(cfg?.IncludeAdultResults == true ? "true" : "false")}" +
                   $"&language={Uri.EscapeDataString(cfg?.TmdbLanguage ?? "en-US")}&page=1";

        using var doc = await GetJsonAsync(path, ct).ConfigureAwait(false);
        var items = new List<TmdbItem>();
        if (!doc.RootElement.TryGetProperty("results", out var results) || results.ValueKind != JsonValueKind.Array)
        {
            return items;
        }

        foreach (var el in results.EnumerateArray())
        {
            var type = mediaType == "all" ? GetString(el, "media_type") : mediaType;
            if (type is not ("movie" or "tv"))
            {
                continue; // skips people
            }

            var item = ToItem(el, type);
            if (item is not null)
            {
                items.Add(item);
            }
        }

        return items;
    }

    public async Task<TmdbItem> GetDetailsAsync(string mediaType, int tmdbId, CancellationToken ct)
    {
        var cfg = Plugin.Instance?.Configuration;
        var path = $"{mediaType}/{tmdbId.ToString(CultureInfo.InvariantCulture)}" +
                   $"?language={Uri.EscapeDataString(cfg?.TmdbLanguage ?? "en-US")}";

        using var doc = await GetJsonAsync(path, ct).ConfigureAwait(false);
        return ToItem(doc.RootElement, mediaType)
               ?? throw new TmdbException("TMDB returned an unexpected response for that title.", 502);
    }

    private static TmdbItem? ToItem(JsonElement el, string type)
    {
        if (!el.TryGetProperty("id", out var idEl) || !idEl.TryGetInt32(out var id))
        {
            return null;
        }

        var title = GetString(el, "title") ?? GetString(el, "name");
        if (string.IsNullOrWhiteSpace(title))
        {
            return null;
        }

        var date = GetString(el, "release_date") ?? GetString(el, "first_air_date");
        var year = date is { Length: >= 4 } ? date[..4] : null;

        double? rating = null;
        if (el.TryGetProperty("vote_average", out var v) && v.ValueKind == JsonValueKind.Number && v.TryGetDouble(out var d) && d > 0)
        {
            rating = Math.Round(d, 1);
        }

        return new TmdbItem(id, type, title, year, GetString(el, "overview"), GetString(el, "poster_path"), rating);
    }

    private static string? GetString(JsonElement el, string name) =>
        el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;

    private async Task<JsonDocument> GetJsonAsync(string pathAndQuery, CancellationToken ct)
    {
        var key = Plugin.Instance?.Configuration.TmdbApiKey?.Trim();
        if (string.IsNullOrEmpty(key))
        {
            throw new TmdbException(
                "Requests aren't set up yet. Ask an admin to add a TMDB API key in the plugin settings.", 503);
        }

        // v4 read access tokens are JWTs; v3 keys are short hex strings.
        var useBearer = key.StartsWith("eyJ", StringComparison.Ordinal);
        var url = $"{ApiBase}/{pathAndQuery}";
        if (!useBearer)
        {
            url += (url.Contains('?') ? "&" : "?") + "api_key=" + Uri.EscapeDataString(key);
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (useBearer)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        }

        var client = _httpClientFactory.CreateClient(NamedClient.Default);
        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, ct).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            // Deliberately not logging the URL: it can contain the API key.
            _logger.LogWarning("TMDB request failed: {Message}", ex.Message);
            throw new TmdbException("Couldn't reach TMDB. Try again in a moment.", 502);
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new TmdbException(
                    "TMDB rejected the API key. An admin needs to check it in the plugin settings.", 502);
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new TmdbException("That title wasn't found on TMDB.", 404);
            }

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("TMDB returned {Status}", (int)response.StatusCode);
                throw new TmdbException("TMDB returned an error. Try again in a moment.", 502);
            }

            var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
            return await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
        }
    }
}
