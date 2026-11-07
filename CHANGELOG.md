# Changelog

## 0.2.1

- Migrated all 22 playground tools to React, TypeScript and Material UI while preserving the existing API contracts.
- Added searchable category navigation, shareable tool URLs, a mobile menu and paginated result tables.
- Preserved Portuguese and English, form values and results when switching languages, and the independent number-to-words output culture.
- Distinguished missing records from unavailable API routes, and retained validation, timeout, provider and rate-limit feedback.
- Added frontend unit tests and Chromium coverage for all tools in both languages, mobile navigation, pagination and request cancellation.
- Updated the production Docker build to compile frontend assets separately; the runtime remains ASP.NET Core 10 without Node.js.
- Documented the Node.js 24 and pnpm 11.25.0 build prerequisites for local development and Visual Studio 2026.

## 0.2.0

- Added a local economic indicator catalog and BCB SGS observations for Selic target, daily CDI and monthly IPCA, with reference dates, explicit units and bounded caching.
- Added a bank directory and COMPE code lookup using BrasilAPI, shared 24-hour caching and explicit handling of institutions without COMPE codes.
- Added IBGE state and municipality lists with replaceable adapters, bounded 24-hour caching and provider failure handling.
- Added a public CEP lookup through a replaceable ViaCEP adapter, bounded local caching and explicit provider error responses.
- Added a responsive Portuguese and English playground for all 22 tools, including public lookups, with formatted results and request/response JSON.
- Enabled Swagger in Development and Homologation while keeping the public playground available in all environments.
- Added environment-specific hosting tests and production container checks for the playground assets.

## 0.1.0

- Added 15 REST utilities for Brazilian documents, finance, percentages, conversions, calendars and text.
- Added reusable Core and Brazil libraries, Swagger UI, health checks, configurable rate limits and CORS.
- Added unit and HTTP integration tests, GitHub Actions CI, a Dockerfile and C#, JavaScript and Python clients.
