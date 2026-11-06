# Changelog

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
