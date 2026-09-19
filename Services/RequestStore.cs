using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Jellyfin.Plugin.MediaRequests.Models;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MediaRequests.Services;

public enum AddOutcome
{
    Added,
    Duplicate,
    LimitReached
}

/// <summary>Keeps requests in memory and persists them to a JSON file in the Jellyfin data folder.</summary>
public sealed class RequestStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly object _gate = new();
    private readonly string _filePath;
    private readonly ILogger<RequestStore> _logger;
    private readonly List<MediaRequest> _items;

    public RequestStore(IApplicationPaths paths, ILogger<RequestStore> logger)
    {
        _logger = logger;
        var dir = Path.Combine(paths.DataPath, "media-requests");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "requests.json");
        _items = Load();
    }

    public static string Key(string mediaType, int tmdbId) => $"{mediaType}:{tmdbId}";

    public IReadOnlyList<MediaRequest> GetAll()
    {
        lock (_gate)
        {
            return _items.ToArray();
        }
    }

    public MediaRequest? Get(Guid id)
    {
        lock (_gate)
        {
            return _items.FirstOrDefault(r => r.Id == id);
        }
    }

    /// <summary>Returns the open (pending or approved) request for each title, keyed by <see cref="Key"/>.</summary>
    public Dictionary<string, MediaRequest> GetActive()
    {
        lock (_gate)
        {
            return _items
                .Where(IsActive)
                .GroupBy(r => Key(r.MediaType, r.TmdbId))
                .ToDictionary(g => g.Key, g => g.OrderBy(r => r.CreatedUtc).First());
        }
    }

    public AddOutcome TryAdd(MediaRequest request, int maxPendingPerUser, out MediaRequest? existing)
    {
        lock (_gate)
        {
            existing = _items.FirstOrDefault(r =>
                IsActive(r) && r.MediaType == request.MediaType && r.TmdbId == request.TmdbId);
            if (existing is not null)
            {
                return AddOutcome.Duplicate;
            }

            if (maxPendingPerUser > 0 &&
                _items.Count(r => r.UserId == request.UserId && r.Status == RequestStatus.Pending) >= maxPendingPerUser)
            {
                return AddOutcome.LimitReached;
            }

            _items.Add(request);
            Save();
            return AddOutcome.Added;
        }
    }

    public MediaRequest? Update(Guid id, Func<MediaRequest, MediaRequest> change)
    {
        lock (_gate)
        {
            var index = _items.FindIndex(r => r.Id == id);
            if (index < 0)
            {
                return null;
            }

            var updated = change(_items[index]);
            _items[index] = updated;
            Save();
            return updated;
        }
    }

    public bool Remove(Guid id)
    {
        lock (_gate)
        {
            var removed = _items.RemoveAll(r => r.Id == id) > 0;
            if (removed)
            {
                Save();
            }

            return removed;
        }
    }

    private static bool IsActive(MediaRequest r) =>
        r.Status is RequestStatus.Pending or RequestStatus.Approved;

    private List<MediaRequest> Load()
    {
        if (!File.Exists(_filePath))
        {
            return new List<MediaRequest>();
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<MediaRequest>>(json, JsonOptions) ?? new List<MediaRequest>();
        }
        catch (Exception ex)
        {
            // Keep the unreadable file so nothing is lost, and start fresh.
            var backup = _filePath + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            _logger.LogError(ex, "Could not read {Path}. Moving it to {Backup} and starting with an empty list.", _filePath, backup);
            try
            {
                File.Move(_filePath, backup);
            }
            catch (IOException)
            {
                // Ignore; the next save overwrites the file.
            }

            return new List<MediaRequest>();
        }
    }

    // Callers hold _gate.
    private void Save()
    {
        try
        {
            var tmp = _filePath + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(_items, JsonOptions));
            File.Move(tmp, _filePath, overwrite: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not save media requests to {Path}", _filePath);
            throw;
        }
    }
}
