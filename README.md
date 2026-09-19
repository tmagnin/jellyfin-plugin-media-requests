# Jellyfin Media Requests

Adds a **Requests** button to the main Jellyfin interface (bottom-right of every page, hidden during video playback).
It opens a panel where users search for a movie or show and request it.
Admins review requests in the same panel: approve, decline (with an optional reason), mark as available, or delete.

- Search is powered by TMDB. The API key stays on the server.
- **Admins get a Settings tab in the panel** for the TMDB API key, language, per-user request limit, and adult results. "Save and test" checks the key works.
- Titles already in the user's library show **Open in library** instead of a Request button.
- Titles someone else has already requested show their status, so nobody makes duplicates.
- Requests switch to **Available** on their own once the title appears in the library (matched by TMDB ID).
- Users can cancel their own pending requests. Admins see every request; users see only their own.
- The button is added by the plugin itself as the web page is served. No other plugins are needed and no Jellyfin files are edited.

Built for **Jellyfin 12.1** (.NET 10).

## Install from a plugin repository (recommended)

Once this project is on GitHub with a release (see "Publishing your own repository" below), add it to Jellyfin once and install and update it like any other plugin:

1. **Dashboard > Plugins > Repositories > +**
2. Name: `Media Requests`. URL: `https://raw.githubusercontent.com/<you>/<repo>/main/manifest.json`
3. **Dashboard > Plugins > Catalog**, find **Media Requests** under General, install it, and restart Jellyfin.
4. Hard-refresh the web page (Ctrl+F5). The **Requests** button appears bottom-right. As an admin, open it and go to **Settings** to add your TMDB key.

## Publishing your own repository

1. Create a **public** GitHub repository and push this project to it.
2. Replace `your-name` in `build.yaml` and `manifest.json` with your name or GitHub username.
3. Publish a version. Any of these works, and versions need four numbers:
   - **On GitHub:** open the **Actions** tab, choose **Release**, click **Run workflow**, and enter a version such as `1.0.0.0`.
   - **From the command line:** `git tag v1.0.0.0 && git push origin v1.0.0.0`
   - **From the Releases page:** create a release with a new tag such as `v1.0.0.0`. The workflow attaches the zip to it.
4. The **Release** workflow builds the plugin, attaches the zip to a GitHub release, and adds the version to `manifest.json` on `main`.
5. For later versions, use `1.0.1.0`, `1.1.0.0`, and so on. Older versions stay in the manifest.

## Build by hand

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0). It can be on any machine, not just the Jellyfin server.
Run this from the folder that contains `Jellyfin.Plugin.MediaRequests.csproj`:

```bash
dotnet publish -c Release -o out
```

No SDK installed? With Docker, from the same folder:

```bash
docker run --rm -v "$PWD":/src -w /src mcr.microsoft.com/dotnet/sdk:10.0 dotnet publish -c Release -o out
```

## Install

1. Create a folder named `MediaRequests` inside your Jellyfin `plugins` directory
   (for example `/var/lib/jellyfin/plugins/MediaRequests` on Linux, or `/config/plugins/MediaRequests` in the official Docker image).
2. Copy `out/Jellyfin.Plugin.MediaRequests.dll` into it.
3. Restart Jellyfin.
4. Go to **Dashboard > Plugins > Media Requests** and paste a TMDB API key
   (free at themoviedb.org: Settings > API). Either the v3 API key or the v4 read access token works.


Requests are stored in `<jellyfin data dir>/media-requests/requests.json`.

## Targeting a different Jellyfin version

Jellyfin plugins are built against one server version. To target another:

| Jellyfin | `TargetFramework` | `Jellyfin.Controller` | `targetAbi` (build.yaml) |
|----------|-------------------|-----------------------|--------------------------|
| 10.10.x  | `net8.0`          | `10.10.*`             | `10.10.0.0`              |
| 10.11.x  | `net9.0`          | `10.11.*`             | `10.11.0.0`              |
| 12.1.x   | `net10.0`         | `12.1.0`              | `12.1.0.0`               |

Change those in `Jellyfin.Plugin.MediaRequests.csproj` and `build.yaml`, then rebuild.

## API

All routes need a signed-in user; the status route needs an administrator.

| Method | Route | Purpose |
|--------|-------|---------|
| GET    | `/MediaRequests/Search?query=&mediaType=all\|movie\|tv` | Search TMDB |
| GET    | `/MediaRequests/Requests` | List requests (all for admins, own for users) |
| POST   | `/MediaRequests/Requests` | Create `{ tmdbId, mediaType, note? }` |
| PUT    | `/MediaRequests/Requests/{id}/Status` | Admin: `{ status, adminNote? }` |
| DELETE | `/MediaRequests/Requests/{id}` | Cancel own pending request, or admin delete |
