# Architecture

The ASP.NET Core API owns HTTP contracts, validation, rate limits and documentation. Core and Brazil are independent .NET 10 libraries. Infrastructure implements external adapters, beginning with ViaCEP for postal address lookups in v0.2 development. The address lookup contract belongs to Brazil, and API composition selects its provider and manages bounded process-local caching. Calculation and validation endpoints use local logic. Postal address lookups require outbound HTTPS access to ViaCEP.
