# Shoko Filter Helper Plugin

A [Shoko](https://shokoanime.com/) plugin that provides lightweight filter endpoints for client-side filtering.

## Endpoints

### TupleIDs — Group-Series Tuples

- `GET /api/plugin/FilterHelper/{filterID}/TupleIDs`
- `POST /api/plugin/FilterHelper/Preview/TupleIDs`

Returns a list of `(GroupID, SeriesID)` pairs for the given filter.

### FilteredIDs — Filtered Group IDs with Chains

- `GET /api/plugin/FilterHelper/{filterID}/FilteredIDs`
- `GET /api/plugin/FilterHelper/{filterID}/Group/{groupID}/FilteredIDs`
- `POST /api/plugin/FilterHelper/Preview/FilteredIDs`
- `POST /api/plugin/FilterHelper/Preview/Group/{groupID}/FilteredIDs`

Returns lightweight group ID results with hierarchy chain information and series IDs, instead of full group objects.

## Installation

### GUI (Recommended)

1. Open the Shoko Web UI and navigate to **Settings → Plugins → Repositories**.
2. Add the manifest URL:
   ```
   https://raw.githubusercontent.com/revam/dotnet-shoko-plugin-filter-helper/stable/manifest.json
   ```
3. Go to **Settings → Plugins → Browse** and find **Filter Helper**.
4. Click **Install** on the desired version.
5. Restart Shoko.

### Manual

1. Download the latest release archive from the [Releases](https://github.com/revam/dotnet-shoko-plugin-filter-helper/releases) page.
2. Extract the `.dll` into your Shoko `plugins` directory.
3. Restart Shoko.

## Building

```bash
dotnet build Shoko.Plugin.FilterHelper.slnx
```
