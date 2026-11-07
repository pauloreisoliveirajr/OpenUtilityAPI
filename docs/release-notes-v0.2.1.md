# OpenUtilityAPI v0.2.1

OpenUtilityAPI 0.2.1 delivers the new React, TypeScript and Material UI playground for all 22 tools. The 15 local POST utilities and seven GET lookup contracts remain unchanged from 0.2.0.

- Searchable category navigation and shareable links to individual tools.
- A responsive mobile menu, address cards, formatted results and paginated tables.
- Portuguese and English with preserved form values and results when changing the interface language.
- Request/response JSON inspection, request cancellation and distinct messages for missing records, missing routes, validation errors, provider failures and rate limits.
- A separate Node.js frontend build stage in Docker; production runs only ASP.NET Core 10.
- Updated Visual Studio 2026 instructions and security documentation for the 0.2 release line.

## Build and upgrade

The .NET target remains .NET 10. Building from source now requires Node.js 24 and pnpm 11.25.0 to compile the frontend before building or publishing .NET. See the [frontend guide](frontend.md). Docker performs this frontend build automatically.

The public page remains at `/`. Swagger UI and OpenAPI JSON remain available only in Development and Homologation. Public data lookups require outbound HTTPS access to their providers. No API caller changes are required.

Core and Brazil NuGet packages accompany the release as downloadable assets for use with a local package source; this does not publish them to NuGet.org.
