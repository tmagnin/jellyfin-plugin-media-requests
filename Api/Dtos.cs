using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Jellyfin.Plugin.MediaRequests.Models;
using Jellyfin.Plugin.MediaRequests.Services;

namespace Jellyfin.Plugin.MediaRequests.Api;

// Every property has an explicit JSON name so the web page always sees camelCase,
// whatever naming policy Jellyfin's MVC options use.

public sealed class ErrorDto
{
    public ErrorDto(string message)
    {
        Message = message;
    }

    [JsonPropertyName("message")]
    public string Message { get; }
}

public sealed class OkDto
{
    [JsonPropertyName("ok")]
    public bool Ok { get; set; } = true;
}

public sealed class SearchResultDto
{
    [JsonPropertyName("tmdbId")]
    public int TmdbId { get; set; }

    [JsonPropertyName("mediaType")]
    public string MediaType { get; set; } = "movie";

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("year")]
    public string? Year { get; set; }

    [JsonPropertyName("overview")]
    public string? Overview { get; set; }

    [JsonPropertyName("posterUrl")]
    public string? PosterUrl { get; set; }

    [JsonPropertyName("rating")]
    public double? Rating { get; set; }

    [JsonPropertyName("inLibrary")]
    public bool InLibrary { get; set; }

    [JsonPropertyName("libraryItemId")]
    public string? LibraryItemId { get; set; }

    /// <summary>Pending or Approved when someone has already requested this title.</summary>
    [JsonPropertyName("requestStatus")]
    public string? RequestStatus { get; set; }

    [JsonPropertyName("requestedByMe")]
    public bool RequestedByMe { get; set; }
}

public sealed class CreateRequestDto
{
    [JsonPropertyName("tmdbId")]
    public int TmdbId { get; set; }

    [JsonPropertyName("mediaType")]
    public string? MediaType { get; set; }

    [JsonPropertyName("note")]
    public string? Note { get; set; }
}

public sealed class UpdateStatusDto
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("adminNote")]
    public string? AdminNote { get; set; }
}

public sealed class RequestDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("tmdbId")]
    public int TmdbId { get; set; }

    [JsonPropertyName("mediaType")]
    public string MediaType { get; set; } = "movie";

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("year")]
    public string? Year { get; set; }

    [JsonPropertyName("overview")]
    public string? Overview { get; set; }

    [JsonPropertyName("posterUrl")]
    public string? PosterUrl { get; set; }

    [JsonPropertyName("userName")]
    public string UserName { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "Pending";

    [JsonPropertyName("note")]
    public string? Note { get; set; }

    [JsonPropertyName("adminNote")]
    public string? AdminNote { get; set; }

    [JsonPropertyName("libraryItemId")]
    public string? LibraryItemId { get; set; }

    [JsonPropertyName("createdUtc")]
    public DateTime CreatedUtc { get; set; }

    [JsonPropertyName("updatedUtc")]
    public DateTime? UpdatedUtc { get; set; }

    public static RequestDto From(MediaRequest r) => new()
    {
        Id = r.Id,
        TmdbId = r.TmdbId,
        MediaType = r.MediaType,
        Title = r.Title,
        Year = r.Year,
        Overview = r.Overview,
        PosterUrl = TmdbClient.PosterUrl(r.PosterPath, "w185"),
        UserName = r.UserName,
        Status = r.Status.ToString(),
        Note = r.Note,
        AdminNote = r.AdminNote,
        LibraryItemId = r.LibraryItemId?.ToString("N"),
        CreatedUtc = DateTime.SpecifyKind(r.CreatedUtc, DateTimeKind.Utc),
        UpdatedUtc = r.UpdatedUtc is { } u ? DateTime.SpecifyKind(u, DateTimeKind.Utc) : null
    };
}

public sealed class RequestListDto
{
    [JsonPropertyName("isAdmin")]
    public bool IsAdmin { get; set; }

    /// <summary>True for admins when no TMDB key has been saved yet.</summary>
    [JsonPropertyName("needsSetup")]
    public bool NeedsSetup { get; set; }

    [JsonPropertyName("requests")]
    public List<RequestDto> Requests { get; set; } = new();
}

public sealed class SettingsDto
{
    /// <summary>The key itself is never sent to the browser.</summary>
    [JsonPropertyName("hasApiKey")]
    public bool HasApiKey { get; set; }

    [JsonPropertyName("language")]
    public string Language { get; set; } = "en-US";

    [JsonPropertyName("maxPendingRequestsPerUser")]
    public int MaxPendingRequestsPerUser { get; set; }

    [JsonPropertyName("includeAdultResults")]
    public bool IncludeAdultResults { get; set; }
}

public sealed class UpdateSettingsDto
{
    /// <summary>Leave empty to keep the saved key.</summary>
    [JsonPropertyName("tmdbApiKey")]
    public string? TmdbApiKey { get; set; }

    [JsonPropertyName("tmdbLanguage")]
    public string? TmdbLanguage { get; set; }

    [JsonPropertyName("maxPendingRequestsPerUser")]
    public int MaxPendingRequestsPerUser { get; set; }

    [JsonPropertyName("includeAdultResults")]
    public bool IncludeAdultResults { get; set; }
}
