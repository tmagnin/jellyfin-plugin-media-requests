using System;

namespace Jellyfin.Plugin.MediaRequests.Models;

public enum RequestStatus
{
    Pending,
    Approved,
    Declined,
    Available
}

public sealed record MediaRequest
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public int TmdbId { get; init; }

    /// <summary>Either "movie" or "tv".</summary>
    public string MediaType { get; init; } = "movie";

    public string Title { get; init; } = string.Empty;

    public string? Year { get; init; }

    public string? Overview { get; init; }

    public string? PosterPath { get; init; }

    public Guid UserId { get; init; }

    public string UserName { get; init; } = string.Empty;

    public string? Note { get; init; }

    public RequestStatus Status { get; init; } = RequestStatus.Pending;

    public string? AdminNote { get; init; }

    public Guid? LibraryItemId { get; init; }

    public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;

    public DateTime? UpdatedUtc { get; init; }
}
