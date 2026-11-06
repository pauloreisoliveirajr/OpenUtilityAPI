# Architecture

The ASP.NET Core API owns HTTP contracts, validation, rate limits and documentation. Core and Brazil are independent .NET 10 libraries. Infrastructure implements external adapters for ViaCEP postal address lookups and IBGE locality lists in v0.2 development. Lookup contracts belong to Brazil, and API composition selects providers and manages bounded process-local caching. Calculation and validation endpoints use local logic. Public address and locality lookups require outbound HTTPS access to their respective providers.
