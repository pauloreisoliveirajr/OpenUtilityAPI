# OpenUtilityAPI

OpenUtilityAPI is a free, self-hosted .NET 10 REST API. Version 0.2.0 offers 15 local utilities and seven public data lookups. Core and Brazil can also be used directly from .NET. Calculations and validations run locally. CEP, locality, bank and indicator observations require outbound HTTPS access to ViaCEP, IBGE, BrasilAPI and BCB SGS, respectively. No credentials are required.

[Português](README.pt-BR.md) · [API contracts](docs/api-contracts.md) · [Architecture](docs/architecture.md) · [Roadmap](docs/roadmap.md)

## Run locally

Clone the repository and open a terminal at its root. Install the .NET 10 SDK, then run:

```sh
dotnet restore
dotnet build -c Release --no-restore
dotnet test -c Release --no-build
dotnet run --project src/OpenUtilityAPI.Api --launch-profile http
```

Open the [web playground](http://localhost:5146/) to try the API in your browser. Follow the steps in [Using the web page](#using-the-web-page).

Open [Swagger UI](http://localhost:5146/swagger) or check [service health](http://localhost:5146/health). The OpenAPI document is at `/swagger/v1/swagger.json`.

### Visual Studio 2026

Install the **ASP.NET and web development** workload and the .NET 10 SDK. Open `OpenUtilityAPI.sln`, set `OpenUtilityAPI.Api` as the startup project, choose the `https` profile and press **F5**. Swagger opens at `https://localhost:7146/swagger`. If needed, run `dotnet dev-certs https --trust` and accept the trust prompt.

## Using the web page

Start the API and open the URL matching your launch method.

| Launch method | Web page |
| --- | --- |
| CLI with the `http` profile | [http://localhost:5146/](http://localhost:5146/) |
| Visual Studio with the `https` profile | [https://localhost:7146/](https://localhost:7146/) |
| Docker with port 8080 mapped | [http://localhost:8080/](http://localhost:8080/) |

1. Choose **Português** or **English** in the header. Your preference is saved in the browser.
2. Select one of the 22 tools from the catalog.
3. Adjust the form values or select **Use example** to restore the sample.
4. Select **Send request** to see the formatted result and HTTP status.
5. Expand **View request and response JSON** to inspect the API payloads.

Switching languages preserves the form values and the current response. The number-to-words output language is selected separately in its form. Use sample data when testing personal documents. Requests use the API's existing rate limits.

The web page is available in every environment. Swagger UI and OpenAPI JSON are available only in `Development` and `Homologation` (HML).

## Call the API

The playground covers 15 POST utilities and seven GET lookups. Public lookups include postal addresses, states, municipalities, the bank directory, COMPE codes, the indicator catalog and the latest indicator observation.

```sh
curl http://localhost:5146/api/v1/br/addresses/01001-000
curl http://localhost:5146/api/v1/br/localities/states/DF/municipalities
curl http://localhost:5146/api/v1/br/banks/001
curl http://localhost:5146/api/v1/br/indicators/ipca
```

External lookup failures return 502 or 504. Unknown records return 404. Successful lookups use bounded process-local caches, with lifetimes documented in the API contracts. The indicator catalog is local. Observations include the reference date and the source's percentage unit. For example, a yearly percentage rate and a daily percentage rate describe different periods.

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
docker build -t openutilityapi:0.2.0 .
docker run --rm -p 127.0.0.1:8080:8080 openutilityapi:0.2.0
```

Open the [web playground](http://localhost:8080/). The default container environment is `Production`. To enable Swagger for local development or HML, add `-e ASPNETCORE_ENVIRONMENT=Development` or `-e ASPNETCORE_ENVIRONMENT=Homologation` before the image name in the run command. Swagger is then available at `/swagger`. Health is available at `/health`.

## Configuration

The API accepts unauthenticated requests. The default fixed window is 120 requests per minute per remote IP. Configure it using `RateLimit__RequestsPerMinute`. Excess requests return 429 Problem Details and a `Retry-After` header when available. Behind a reverse proxy the API sees the proxy's address by default.

Browser cross-origin access is disabled by default. Set explicit origins using `Cors__Origins__0`, `Cors__Origins__1` and so on. Kestrel limits request bodies to 64 KiB. Swagger UI and its JSON document are available only when ASPNETCORE_ENVIRONMENT is Development or Homologation (HML). The playground at / is available in every environment. The environment name Homologation is explicit; Staging does not enable Swagger. Configure network access to HML according to your deployment.

## Local NuGet packages

```sh
dotnet pack src/OpenUtilityAPI.Core -c Release -o artifacts/packages
dotnet pack src/OpenUtilityAPI.Brazil -c Release -o artifacts/packages
```

Add `artifacts/packages` as a local NuGet source and reference `OpenUtilityAPI.Core` or `OpenUtilityAPI.Brazil` at version `0.2.0`. For example, `OpenUtilityAPI.Core.Finance.Interest(1000m, 0.01m, 12, 2, true)` returns a compound interest result without HTTP.

[Contributing](CONTRIBUTING.md) · [Security](SECURITY.md) · [Changelog](CHANGELOG.md) · [MIT license](LICENSE)
