# Amplyst - Spotify API

A .NET 10 minimal API that connects to the Spotify Web API. It manages OAuth 2.0 authentication and retrieves playlist data for the authenticated user.

**Current scope (August 2026):** This API supports the Spotify Web API as it operates in August 2026.
The working functionality adds artists, items (tracks/show episodes), and playlists, based on the current user playlists, in a fire-and-forget manner, respecting Spotify's Retry-After header.

**Planned scope:** The final goal is a full cloud music library management dashboard. This includes filtering, ordering, and managing music content across cloud services.

---

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- A Spotify Premium account (See why [here](https://developer.spotify.com/blog/2026-02-06-update-on-developer-access-and-platform-security))
- A Spotify Developer account with a registered application with `Client ID` and `Client Secret`

---

## Configuration

Set the following values before you run the application:

| Variable        | Description                              |
|-----------------|------------------------------------------|
| `clientId`      | The Client ID from your Spotify application |
| `clientSecret`  | The Client Secret from your Spotify application |
| `environmentUrl`| The base URL of this server (default: `https://127.0.0.1:7138`) |

To use .NET user secrets in development, run:

```bash
dotnet user-secrets init
dotnet user-secrets set "Spotify:ClientId" "<your-client-id>"
dotnet user-secrets set "Spotify:ClientSecret" "<your-client-secret>"
```

Set the redirect URI in your Spotify application dashboard to:

```
https://127.0.0.1:7138/api/v1/auth/callback
```

See [Spotify's Documentation on Redirect URIs](https://developer.spotify.com/documentation/general/guides/authorization-guide/#redirect-uris) for more information.

---

## Run the Application

Locally:

```bash
dotnet run --launch-profile https
```

Docker:

```bash
docker build -t amplyst-spotify-api .
docker run -p 7138:7138 -e Spotify__ClientId="<your-client-id>" -e Spotify__ClientSecret="<your-client-secret>" amplyst-spotify-api
```

The server should start at the configured `Properties/launchSettings.json` url (default is `https://127.0.0.1:7138`).

---

## Endpoints

### `GET /api/v1/auth`

Starts the Spotify OAuth 2.0 authorization flow. Redirects the user to the Spotify login page.

**Response:** `302 Redirect` to `https://accounts.spotify.com/authorize`

---

### `GET /api/v1/auth/callback`

Receives the authorization code from Spotify. Exchanges the code for an access token.

> **Note:** Do not call this endpoint directly. Use `GET /api/v1/auth` to start the authorization flow. Spotify calls `/api/v1/auth/callback` automatically after the user authenticates. Direct calls will fail because the required `state` value is only set by `/api/v1/auth`.

| Query Parameter | Required | Description                              |
|-----------------|----------|------------------------------------------|
| `code`          | Yes      | The authorization code from Spotify      |
| `state`         | Yes      | The state value from the `/auth` request |
| `error`         | No       | An error string returned by Spotify      |

**Success response (`200 OK`):**

`2026-01-01T00:00:00Z`

**Error responses:**
- `400 Bad Request` — Missing code, state mismatch, or Spotify returned an error
- `401 Unauthorized` — Access token is missing or expired

---

### `POST /api/v1/import`

Creates a new import job for the authenticated user. The job will import the user's playlists, artists, and items (tracks/show episodes) from Spotify.

Requires the user to be authenticated via the `/api/v1/auth` endpoint.

**Response:** `202 Accepted` with a JSON body containing the job ID and status.

**Error responses:**
- `401 Unauthorized` — User is not authenticated
- `409 Conflict` — An import job is already in progress for the user.

---

### `GET /api/v1/import/jobs/{jobId}`

Returns the status of an import job.

| Path Parameter  | Required  | Description               |
|-----------------|-----------|---------------------------|
| `jobId`         | Yes       | The ID of the import job  |

**Response:** `200 OK` with a JSON body containing the job status.

**Error responses:**
- `401 Unauthorized` — User is not authenticated
- `404 Not Found` — Import job with the specified ID does not exist

---