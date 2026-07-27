# Rich House Development Guide

This repository contains the Rich House backend and frontend:

- Backend: `MarketifyPlatForm/Marketify`
- Frontend: `MarkitefayFrontedIn`

Default local URLs:

- Backend API: `http://localhost:4000`
- Frontend: `http://localhost:4200`
- Backend health check: `http://localhost:4000/health`

## Prerequisites

- Linux: Debian, Ubuntu, or Parrot OS
- `.NET SDK 9`
- `Node.js` and `npm`
- `curl`
- `docker` if you want `./dev.sh` to start the expected local SQL Server container automatically
- SQL Server reachable by the backend connection strings

## Database Requirements

The backend needs two SQL Server databases:

- `MarketifyDB`
- `HangfireDb`

By default the backend reads connection strings from:

1. Environment variables such as `ConnectionStrings__DefaultConnection`
2. `.NET user-secrets`
3. `appsettings.Development.json`
4. `appsettings.json`

The checked-in `appsettings.json` uses placeholders for the SQL password, so another developer should set real values with user-secrets or environment variables before running the API.

Recommended setup from the backend folder:

```bash
cd "MarketifyPlatForm/Marketify"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=MarketifyDB;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;Encrypt=False"
dotnet user-secrets set "ConnectionStrings:HangfireConnection" "Server=localhost,1433;Database=HangfireDb;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True;Encrypt=False"
dotnet user-secrets set "Jwt:key" "replace-with-a-long-random-secret"
```

If you use the included launcher, it expects the local SQL Server Docker container name to be `marketify-sql`. If SQL Server is not available, `./dev.sh` stops and prints a message describing what is missing.

## Frontend API URL Configuration

The frontend API base URL is centralized in Angular environment files:

- Development: `MarkitefayFrontedIn/src/environments/environment.development.ts`
- Production/deployment builds: `MarkitefayFrontedIn/src/environments/environment.ts`

For local development the default is:

```ts
baseUrl: 'http://localhost:4000'
```

You only need to change that single environment value instead of editing multiple services.

## Install Dependencies

Backend:

```bash
cd "MarketifyPlatForm/Marketify"
dotnet restore
dotnet build
```

Frontend:

```bash
cd "MarkitefayFrontedIn"
npm install
npm run build
```

## Run Everything With One Command

From the common project root:

```bash
chmod +x dev.sh stop-dev.sh
./dev.sh
```

What `./dev.sh` does:

- resolves its own directory safely, even if the path contains spaces
- checks for port conflicts on `4000` and `4200`
- avoids starting duplicate frontend or backend processes
- starts the backend first
- waits for `http://localhost:4000/health`
- starts the Angular frontend after the backend is ready
- writes PID files under `.pids/`
- writes logs under `.logs/`

Useful log files:

- `.logs/startup.log`
- `.logs/backend.log`
- `.logs/frontend.log`

## Stop Everything Cleanly

From the common project root:

```bash
./stop-dev.sh
```

This stops the managed backend and frontend processes cleanly. By default it leaves SQL Server running, matching the existing local scripts.

## Run Manually

Backend:

```bash
cd "MarketifyPlatForm/Marketify"
ASPNETCORE_URLS="http://localhost:4000" dotnet run --no-launch-profile
```

Frontend:

```bash
cd "MarkitefayFrontedIn"
npm start
```

## Troubleshooting

- `ERR_CONNECTION_REFUSED` from the frontend usually means the backend is not running on `http://localhost:4000`. Start it again with `./dev.sh` or run the backend manually.
- If `./dev.sh` reports that port `4000` or `4200` is already in use, stop the old process first or use `./stop-dev.sh` if the launcher started it.
- If the backend fails during startup, check `.logs/backend.log` and confirm SQL Server is reachable with valid credentials.
- If SQL Server is missing, start the `marketify-sql` container or update the backend connection strings to point to a running SQL Server instance.
- If login or profile requests redirect back to `/login`, the stored token is invalid or expired. Sign in again.
- If the frontend cannot reach the API after changing environments, verify the URL in `src/environments/environment.development.ts`.

## After Restarting The Computer

After a reboot or shutdown:

1. Make sure Docker and SQL Server are available if you rely on the local container.
2. Run `./dev.sh` from the project root.
3. Open `http://localhost:4200`.

If the frontend opens before the backend is ready, reload after `./dev.sh` reports the API and website URLs.

## For Another Developer Receiving This Folder

1. Install `.NET 9`, Node.js, npm, curl, and optionally Docker.
2. Open the root project folder exactly as copied; no user-specific paths are required.
3. Set backend secrets for SQL Server and `Jwt:key`.
4. Run backend restore/build and frontend install/build once.
5. Make `dev.sh` and `stop-dev.sh` executable.
6. Start the stack with `./dev.sh`.
7. Stop it later with `./stop-dev.sh`.
