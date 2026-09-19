using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.MediaRequests.Models;
using Jellyfin.Plugin.MediaRequests.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.MediaRequests.Api;

[ApiController]
[Authorize]
[Route("MediaRequests")]
[Produces(MediaTypeNames.Application.Json)]
public class MediaRequestsController : ControllerBase
{
    private const int MaxNoteLength = 500;
    private static long _lastSyncTicks;

    private readonly TmdbClient _tmdb;
    private readonly RequestStore _store;
    private readonly LibraryChecker _library;

    public MediaRequestsController(TmdbClient tmdb, RequestStore store, LibraryChecker library)
    {
        _tmdb = tmdb;
        _store = store;
        _library = library;
    }

    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirst("Jellyfin-UserId")?.Value, out var id) ? id : Guid.Empty;

    private bool IsAdmin => User.IsInRole("Administrator");

    /// <summary>Searches TMDB and marks which results are already in the library or requested.</summary>
    [HttpGet("Search")]
    public async Task<ActionResult<List<SearchResultDto>>> Search(
        [FromQuery] string? query,
        [FromQuery] string? mediaType,
        CancellationToken cancellationToken)
    {
        query = (query ?? string.Empty).Trim();
        if (query.Length < 2)
        {
            return Ok(new List<SearchResultDto>());
        }

        IReadOnlyList<TmdbItem> found;
        try
        {
            found = await _tmdb.SearchAsync(query, NormalizeType(mediaType) ?? "all", cancellationToken);
        }
        catch (TmdbException ex)
        {
            return StatusCode(ex.StatusCode, new ErrorDto(ex.Message));
        }

        var userId = CurrentUserId;
        var active = _store.GetActive();
        var results = new List<SearchResultDto>(found.Count);

        foreach (var item in found)
        {
            var dto = new SearchResultDto
            {
                TmdbId = item.TmdbId,
                MediaType = item.MediaType,
                Title = item.Title,
                Year = item.Year,
                Overview = item.Overview,
                PosterUrl = TmdbClient.PosterUrl(item.PosterPath),
                Rating = item.Rating
            };

            var libraryId = _library.Find(item.MediaType, item.TmdbId, userId);
            if (libraryId is { } lid)
            {
                dto.InLibrary = true;
                dto.LibraryItemId = lid.ToString("N");
            }
            else if (active.TryGetValue(RequestStore.Key(item.MediaType, item.TmdbId), out var existing))
            {
                dto.RequestStatus = existing.Status.ToString();
                dto.RequestedByMe = existing.UserId == userId;
            }

            results.Add(dto);
        }

        return Ok(results);
    }

    /// <summary>Admins get every request; everyone else gets their own.</summary>
    [HttpGet("Requests")]
    public ActionResult<RequestListDto> List()
    {
        SyncAvailability();

        var admin = IsAdmin;
        var userId = CurrentUserId;

        var items = _store.GetAll()
            .Where(r => admin || r.UserId == userId)
            .OrderBy(r => r.Status == RequestStatus.Pending ? 0 : 1)
            .ThenByDescending(r => r.CreatedUtc)
            .Select(RequestDto.From)
            .ToList();

        return Ok(new RequestListDto { IsAdmin = admin, Requests = items });
    }

    [HttpPost("Requests")]
    public async Task<ActionResult<RequestDto>> Create(
        [FromBody] CreateRequestDto body,
        CancellationToken cancellationToken)
    {
        var userId = CurrentUserId;
        if (userId == Guid.Empty)
        {
            return StatusCode(403, new ErrorDto("Sign in as a user to make requests."));
        }

        var type = NormalizeType(body.MediaType);
        if (type is null or "all" || body.TmdbId <= 0)
        {
            return BadRequest(new ErrorDto("Choose a movie or show to request."));
        }

        if (_library.Find(type, body.TmdbId, userId) is not null)
        {
            return Conflict(new ErrorDto("That title is already in the library."));
        }

        TmdbItem details;
        try
        {
            details = await _tmdb.GetDetailsAsync(type, body.TmdbId, cancellationToken);
        }
        catch (TmdbException ex)
        {
            return StatusCode(ex.StatusCode, new ErrorDto(ex.Message));
        }

        var note = body.Note?.Trim();
        if (note is { Length: > MaxNoteLength })
        {
            note = note[..MaxNoteLength];
        }

        var request = new MediaRequest
        {
            TmdbId = details.TmdbId,
            MediaType = type,
            Title = details.Title,
            Year = details.Year,
            Overview = details.Overview,
            PosterPath = details.PosterPath,
            UserId = userId,
            UserName = User.Identity?.Name ?? "Unknown",
            Note = string.IsNullOrEmpty(note) ? null : note
        };

        var max = Plugin.Instance?.Configuration.MaxPendingRequestsPerUser ?? 0;
        switch (_store.TryAdd(request, max, out var existing))
        {
            case AddOutcome.Duplicate:
                return Conflict(new ErrorDto(existing?.UserId == userId
                    ? "You've already requested this."
                    : "Someone has already requested this."));
            case AddOutcome.LimitReached:
                return StatusCode(429, new ErrorDto(
                    $"You have {max} pending requests. Wait for some to be reviewed before adding more."));
            default:
                return Ok(RequestDto.From(request));
        }
    }

    /// <summary>Admin only: approve, decline, reopen, or mark a request as available.</summary>
    [HttpPut("Requests/{id:guid}/Status")]
    [Authorize(Policy = "RequiresElevation")]
    public ActionResult<RequestDto> SetStatus([FromRoute] Guid id, [FromBody] UpdateStatusDto body)
    {
        if (!Enum.TryParse<RequestStatus>(body.Status, ignoreCase: true, out var status)
            || !Enum.IsDefined(status))
        {
            return BadRequest(new ErrorDto("Status must be Pending, Approved, Declined, or Available."));
        }

        var adminNote = body.AdminNote?.Trim();
        if (adminNote is { Length: > MaxNoteLength })
        {
            adminNote = adminNote[..MaxNoteLength];
        }

        var updated = _store.Update(id, r => r with
        {
            Status = status,
            AdminNote = string.IsNullOrEmpty(adminNote) ? null : adminNote,
            UpdatedUtc = DateTime.UtcNow
        });

        return updated is null
            ? NotFound(new ErrorDto("That request no longer exists."))
            : Ok(RequestDto.From(updated));
    }

    /// <summary>Users can cancel their own pending requests; admins can delete any request.</summary>
    [HttpDelete("Requests/{id:guid}")]
    public ActionResult<OkDto> Delete([FromRoute] Guid id)
    {
        var request = _store.Get(id);
        if (request is null)
        {
            return NotFound(new ErrorDto("That request no longer exists."));
        }

        if (!IsAdmin && (request.UserId != CurrentUserId || request.Status != RequestStatus.Pending))
        {
            return StatusCode(403, new ErrorDto("You can only cancel your own pending requests."));
        }

        _store.Remove(id);
        return Ok(new OkDto());
    }

    private static string? NormalizeType(string? value) => value?.Trim().ToLowerInvariant() switch
    {
        "movie" or "movies" => "movie",
        "tv" or "show" or "shows" or "series" => "tv",
        "all" or "" or null => "all",
        _ => null
    };

    /// <summary>Marks open requests as available once the title shows up in the library.</summary>
    private void SyncAvailability()
    {
        var now = DateTime.UtcNow.Ticks;
        if (now - Interlocked.Read(ref _lastSyncTicks) < TimeSpan.FromSeconds(30).Ticks)
        {
            return;
        }

        Interlocked.Exchange(ref _lastSyncTicks, now);

        var open = _store.GetAll()
            .Where(r => r.Status is RequestStatus.Pending or RequestStatus.Approved);

        foreach (var r in open)
        {
            if (_library.Find(r.MediaType, r.TmdbId, null) is not { } libraryId)
            {
                continue;
            }

            _store.Update(r.Id, x => x with
            {
                Status = RequestStatus.Available,
                LibraryItemId = libraryId,
                UpdatedUtc = DateTime.UtcNow
            });
        }
    }
}
