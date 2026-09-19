using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.MediaRequests.Services;

/// <summary>Checks whether a TMDB title is already in the Jellyfin library.</summary>
public sealed class LibraryChecker
{
    private readonly ILibraryManager _libraryManager;
    private readonly IUserManager _userManager;

    public LibraryChecker(ILibraryManager libraryManager, IUserManager userManager)
    {
        _libraryManager = libraryManager;
        _userManager = userManager;
    }

    /// <summary>
    /// Returns the library item id if the title exists. When a user id is given, only items
    /// that user is allowed to see count.
    /// </summary>
    public Guid? Find(string mediaType, int tmdbId, Guid? userId)
    {
        var user = userId is { } id && id != Guid.Empty ? _userManager.GetUserById(id) : null;
        var query = user is null ? new InternalItemsQuery() : new InternalItemsQuery(user);

        query.IncludeItemTypes = new[] { mediaType == "tv" ? BaseItemKind.Series : BaseItemKind.Movie };
        query.HasAnyProviderId = new Dictionary<string, string>
        {
            [MetadataProvider.Tmdb.ToString()] = tmdbId.ToString(CultureInfo.InvariantCulture)
        };
        query.Recursive = true;
        query.Limit = 1;

        var items = _libraryManager.GetItemList(query);
        return items.Count > 0 ? items[0].Id : null;
    }
}
