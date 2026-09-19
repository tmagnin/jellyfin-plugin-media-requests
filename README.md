# Jellyfin Media Requests

Adds a **Requests** entry to the Jellyfin sidebar. Users search for a movie or show and request it.
Admins review requests on the same page: approve, decline (with an optional reason), mark as available, or delete.

- Search is powered by TMDB. The API key stays on the server.
- Titles already in the user's library show **Open in library** instead of a Request button.
- Titles someone else has already requested show their status, so nobody makes duplicates.
- Requests switch to **Available** on their own once the title appears in the library (matched by TMDB ID).
- Users can cancel their own pending requests. Admins see every request; users see only their own.

Built for **Jellyfin 12.1** (.NET 10).

## Install from a plugin repository (recommended)

Once this project is on GitHub with a release (see "Publishing your own repository" below), add it to Jellyfin once and install and update it like any other plugin:

1. **Dashboard > Plugins > Repositories > +**
2. Name: `Media Requests`. URL: `https://raw.githubusercontent.com/<you>/<repo>/main/manifest.json`
3. **Dashboard > Plugins > Catalog**, find **Media Requests** under General, install it, and restart Jellyfin.
