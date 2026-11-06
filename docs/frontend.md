# Web playground

The public interface uses React, TypeScript and Material UI. All 22 tools use the existing API contracts: 15 local POST utilities and seven GET lookups. The application is served at `/` by ASP.NET Core in every environment. Swagger remains restricted to Development and Homologation.

The catalogue supports category navigation, search and a mobile drawer. Each tool has a shareable hash URL, such as `/#cep` or `/#compound`. The language selector changes the interface between Portuguese and English without resetting inputs or the current response. The number-to-words output culture remains an independent form choice. Only the language preference is saved in browser storage; inputs and results are not persisted by the interface.

Results include address cards, localised numbers and dates, indicator units/reference dates and paginated tables. Pagination changes only the displayed rows; the complete response remains available in the JSON details. The UI distinguishes absent records, missing routes, invalid inputs, provider failures, rate limits and network errors. Switching tools or editing inputs cancels the current request; late responses are discarded. Requests have a 20-second client timeout.

## Build for Visual Studio or dotnet

Install Node.js 24 and pnpm 11.25.0 (`npm install --global pnpm@11.25.0`). From the repository root:

```powershell
pnpm --dir frontend/playground install --frozen-lockfile
pnpm --dir frontend/playground test
pnpm --dir frontend/playground build
dotnet build -c Release
dotnet run --project src/OpenUtilityAPI.Api --launch-profile http
```

Open [the playground](http://localhost:5146/). With Visual Studio's HTTPS profile, open [https://localhost:7146/](https://localhost:7146/).

The frontend build generates ignored files in `src/OpenUtilityAPI.Api/wwwroot/`. This directory contains only generated output and is cleaned by Vite when rebuilding. Edit sources under `frontend/playground/`, not files in `wwwroot/`. Build the frontend before building, testing or publishing .NET, including on a fresh clone. A plain `dotnet build` does not run pnpm. Rebuild after changing frontend sources, then refresh the page; restart the API if necessary.

## Frontend development

Start the .NET API and then run:

```powershell
pnpm --dir frontend/playground dev
```

Open [http://127.0.0.1:5173/](http://127.0.0.1:5173/). Vite proxies `/api` to `http://localhost:5146`, so no CORS changes are needed. Set `API_PROXY_TARGET` before starting Vite to use another API address. Fonts and SVG icons are bundled locally.

## Browser tests

After building the frontend and .NET in Release:

```powershell
pnpm --dir frontend/playground exec playwright install chromium
pnpm --dir frontend/playground test:e2e
```

Playwright starts its own .NET API in Production on port 5189. Keep that port free. Set `PLAYWRIGHT_CHANNEL=msedge` to use an installed Edge browser instead of downloaded Chromium. Tests execute all 22 tools in both languages: local utilities use the real backend, while external public sources use controlled HTTP fixtures to make CI independent of provider availability. Provider adapters also have unit and integration coverage in .NET.

## Publication

CI builds the frontend before .NET and runs frontend, backend and browser tests. Docker uses a separate Node build stage and copies the static output into the .NET publication. The runtime image remains ASP.NET Core 10 and does not run Node.js. Production container checks verify HTML and generated JavaScript/CSS assets, API execution and Swagger restrictions.
