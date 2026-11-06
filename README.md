# OpenUtilityAPI

OpenUtilityAPI is a free, self-hosted .NET 10 REST API. Version 0.1.0 provides 15 utilities for Brazilian documents, finance, math, conversions, dates and text. Core and Brazil can also be used directly from .NET. No credentials or external services are required.

[Português](README.pt-BR.md) · [API contracts](docs/api-contracts.md) · [Architecture](docs/architecture.md) · [Roadmap](docs/roadmap.md)

## Run locally

Clone the repository and open a terminal at its root. Install the .NET 10 SDK, then run:

```sh
dotnet restore
dotnet build -c Release --no-restore
dotnet test -c Release --no-build
dotnet run --project src/OpenUtilityAPI.Api --launch-profile http
```

Open the [public playground](http://localhost:5146/) to try all 15 utilities with Portuguese or English forms and formatted results. Use the language selector in the header; your preference is saved in this browser. Changing the interface language preserves inputs and responses. The number-to-words output culture is chosen separately in its form. Expand the JSON panel to inspect requests and responses. Calls use the same API and rate limits.

Open [Swagger UI](http://localhost:5146/swagger) or check [service health](http://localhost:5146/health). The OpenAPI document is at `/swagger/v1/swagger.json`.

### Visual Studio 2026

Install the **ASP.NET and web development** workload and the .NET 10 SDK. Open `OpenUtilityAPI.sln`, set `OpenUtilityAPI.Api` as the startup project, choose the `https` profile and press **F5**. Swagger opens at `https://localhost:7146/swagger`. If needed, run `dotnet dev-certs https --trust` and accept the trust prompt.

## Call the API

```sh
curl -H 'Content-Type: application/json' --data '{"principal":1000,"ratePerPeriod":0.01,"periods":12}' http://localhost:5146/api/v1/finance/compound-interest
```

With the API running, try the examples:

```sh
dotnet run --project examples/CSharp/CSharp.csproj
node examples/JavaScript/compound-interest.mjs
python examples/Python/compound_interest.py
```

The examples accept the base URL as their first argument and use `http://localhost:5146` by default. JavaScript requires Node.js with built-in fetch. Python uses the standard library.

## Docker

```sh
docker build -t openutilityapi:0.1.0 .
docker run --rm -p 127.0.0.1:8080:8080 -e ASPNETCORE_ENVIRONMENT=Development openutilityapi:0.1.0
```

Open `http://localhost:8080/swagger`. The environment flag enables Swagger for local development. The default container environment is Production. Health is available at `/health`.

## Configuration

The API accepts unauthenticated requests. The default fixed window is 120 requests per minute per remote IP. Configure it using `RateLimit__RequestsPerMinute`. Excess requests return 429 Problem Details and a `Retry-After` header when available. Behind a reverse proxy the API sees the proxy's address by default.

Browser cross-origin access is disabled by default. Set explicit origins using `Cors__Origins__0`, `Cors__Origins__1` and so on. Kestrel limits request bodies to 64 KiB. Swagger UI and its JSON document are available only when ASPNETCORE_ENVIRONMENT is Development or Homologation (HML). The playground at / is available in every environment. The environment name Homologation is explicit; Staging does not enable Swagger. Configure network access to HML according to your deployment.

## Local NuGet packages

```sh
dotnet pack src/OpenUtilityAPI.Core -c Release -o artifacts/packages
dotnet pack src/OpenUtilityAPI.Brazil -c Release -o artifacts/packages
```

Add `artifacts/packages` as a local NuGet source and reference `OpenUtilityAPI.Core` or `OpenUtilityAPI.Brazil` at version `0.1.0`. For example, `OpenUtilityAPI.Core.Finance.Interest(1000m, 0.01m, 12, 2, true)` returns a compound interest result without HTTP.

[Contributing](CONTRIBUTING.md) · [Security](SECURITY.md) · [Changelog](CHANGELOG.md) · [MIT license](LICENSE)
