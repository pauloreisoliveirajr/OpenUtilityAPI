# API contracts

All POST routes use camelCase JSON. Invalid requests return HTTP 400 with problem details. The rate limiter returns 429. Unexpected failures return 500 with generic problem details.

## Brazilian documents

Under `/api/v1/br/documents`, POST to `cpf/validate`, `cnpj/validate` or `pis/validate` with `{"value":"12ABC34501DE35"}` (CNPJ example). Use ASCII characters in the compact or standard punctuated format. CNPJ accepts ASCII letters in the first twelve positions and requires decimal check digits. Bad syntax returns 400. A check-digit mismatch or repeated-digit sequence returns 200 with `isValid: false` and a reason. Checks are mathematical only.

POST to `format` with `{"type":"cnpj","value":"12abc34501de35"}` to receive `{"type":"cnpj","formatted":"12.ABC.345/01DE-35"}`. Supported types are `cpf` and `cnpj`. Formatting retains leading zeros, uppercases ASCII CNPJ letters and does not validate check digits.

The alphanumeric checksum follows the [Receita Federal technical manual](https://www.gov.br/receitafederal/pt-br/centrais-de-conteudo/publicacoes/documentos-tecnicos/cnpj).

## Finance and math

POST to `/api/v1/finance/simple-interest`, `compound-interest`, `price` or `sac` (under the same finance prefix) with `{"principal":1000,"ratePerPeriod":0.01,"periods":12,"decimalPlaces":2}`. A rate of 0.01 represents 1% per period. Use a principal from 0 to 10¹², a rate from 0 to 0.05, and 0–360 periods (1–360 for loans). Decimal places default to 2 and may be 0–6. All monetary arithmetic uses decimal and outputs round midpoint values away from zero.

Simple interest uses P × (1 + i × n). Compound interest uses P × (1 + i)ⁿ. Both return final and interest amounts. Price divides principal by the sum of the discount factors for periods 1 through n. SAC amortizes principal evenly over n periods. Schedules use unrounded balances internally, round displayed balances and interest, derive amortization from consecutive displayed balances, and sum displayed installments. This keeps final balance at zero and reconciles the displayed totals. Loan principal must fit the requested scale. No fees or taxes are included.

POST to `/api/v1/math/percentage` with `{"operation":"percentOf","value":200,"operand":15,"decimalPlaces":2}`. Operations are `percentOf` (value × operand ÷ 100), `ratioPercent` (value ÷ operand × 100), and `changePercent` ((operand − value) ÷ |value| × 100). Operands are limited to ±10¹². Zero divisors and unrepresentable results return 400.

## Conversions

- `POST /api/v1/converters/units` with `{"value":32,"fromUnit":"F","toUnit":"C","decimalPlaces":6}`. Use case-sensitive units from one category: length (`mm`, `cm`, `m`, `km`, `in`, `ft`, `mi`), mass (`g`, `kg`, `lb`, `oz`), or temperature (`C`, `F`, `K`). Inputs are limited to ±10¹². Temperatures below absolute zero are rejected. The result uses decimal arithmetic and rounds midpoints away from zero at 0–12 decimal places (default 6).
- `POST /api/v1/converters/number-to-words` with `{"number":42,"culture":"pt-BR"}`. The response includes `words`. Accepted cultures are `pt-BR` and `en-US` and the inclusive integer range is −999999 to 999999.

## Dates

- `POST /api/v1/datetime/difference` with `{"start":"2024-02-28","end":"2024-03-01"}`. Returns `days` as the signed Gregorian calendar difference (end minus start). Equal dates yield zero.
- `POST /api/v1/datetime/add-business-days` with `{"start":"2024-03-01","days":1,"holidays":["2024-03-04"]}`. Returns the resulting ISO date in `result`. Positive offsets move forward and negative offsets backward. The start is excluded from counting. Saturdays, Sundays and supplied holidays are skipped. A zero offset returns the original date. The offset is limited to ±10000 and the optional holiday array to 366 entries. Crossing the supported date range returns 400.

## Text

- `POST /api/v1/text/normalize` with `{"text":" Café  ","trim":true,"collapseWhitespace":true,"removeAccents":true,"letterCase":"upper"}`. Returns the transformed `text`. Options run in the order shown and default to preserving the input. Trimming removes surrounding whitespace, collapsing replaces whitespace runs with one ASCII space, and accent removal strips nonspacing marks after Unicode decomposition. Case conversion uses invariant casing (`preserve`, `lower`, `upper`). Input is limited to 10000 UTF-16 code units.
- `POST /api/v1/text/email/validate` with `{"value":"person@example.com"}`. Returns `isValid` and `reason`. Supports ASCII dot-atom local parts of at most 64 characters and domains with at least two DNS-style labels. Total length is limited to 254 characters. Quoted local parts, display names and Unicode addresses are outside this syntax profile. Validation performs no network lookups.

## Postal address lookup (v0.2 development)

GET /api/v1/br/addresses/{postalCode} accepts eight ASCII digits or the standard 00000-000 mask. The response contains postalCode (eight digits), street, complement, neighborhood, city, state (UF) and ibgeCode. Some CEPs have no street-level information, so street, complement and neighborhood can be empty.

The default adapter calls the [ViaCEP JSON service](https://viacep.com.br/) over HTTPS. This lookup requires outbound connectivity to that free external provider. Existing calculation and validation endpoints remain local.

Invalid syntax returns 400 before contacting the provider. An unknown CEP returns 404. Provider HTTP failures, malformed data and mismatched postal codes return 502. Provider timeouts and a busy outbound lookup queue return 504. Error responses use Problem Details. Request cancellation propagates to the outbound call.

Successful lookups are cached in process for one hour and missing CEPs for five minutes. The cache holds at most 1024 entries, is cleared on restart and is independent in each process. Failures are not cached. Cache misses share one outbound slot to reduce provider load, with a maximum five-second wait for that slot. The HTTP client has a five-second timeout, a 32 KiB response buffer limit and redirects disabled. No automatic retries are performed. This endpoint uses the existing API rate limit.

Example request:

```sh
curl http://localhost:5146/api/v1/br/addresses/01001-000
```

Example response:

```json
{
  "postalCode": "01001000",
  "street": "Praça da Sé",
  "complement": "lado ímpar",
  "neighborhood": "Sé",
  "city": "São Paulo",
  "state": "SP",
  "ibgeCode": "3550308"
}
```

## Brazilian localities (v0.2 development)

GET /api/v1/br/localities/states returns all 26 states and the Federal District as an array of objects containing id, abbreviation, name, regionId and regionName.

GET /api/v1/br/localities/states/{state}/municipalities returns an array of objects containing id (the seven-digit IBGE municipality code), name and state (the uppercase UF abbreviation). The state parameter accepts a recognized two-letter abbreviation in either case, such as SP, sp or DF. Unknown abbreviations, numeric state IDs, surrounding whitespace and multiple-state requests return 400 before any provider call.

Both lists are sorted by name using pt-BR collation with ID as a tie breaker. Names are returned as published by IBGE. The Federal District follows the municipality-equivalent records supplied by IBGE.

The default adapter uses the official [IBGE Localities API](https://servicodados.ibge.gov.br/api/docs/localidades) over HTTPS. It requests the flattened municipality representation to avoid depending on legacy regional subdivisions. Provider HTTP errors, malformed or empty responses, duplicate IDs, incomplete state lists and state/municipality mismatches return 502. Provider timeouts and a busy lookup queue return 504. Errors use Problem Details, and request cancellation propagates to the provider.

Successful lists are cached for 24 hours in a process-local cache with 28 entries (one state list plus one list per UF). Restarting clears the cache. Failures are not cached. Cache misses share one outbound slot, with a maximum five-second wait for that slot. The client has a five-second HTTP timeout, a 2 MiB response buffer limit and redirects disabled. Municipality responses are limited to 1000 records per state. No automatic retries are performed. Existing API rate limits apply.

Example requests:

```sh
curl http://localhost:5146/api/v1/br/localities/states
curl http://localhost:5146/api/v1/br/localities/states/DF/municipalities
```

Example municipality response:

```json
[
  { "id": 5300108, "name": "Brasília", "state": "DF" }
]
```

## Bank directory (v0.2 development)

GET /api/v1/br/banks returns an array of bank and financial institution records containing ispb (eight ASCII digits, including leading zeros), code (numeric COMPE code or null), name and fullName. The list is sorted by name using pt-BR collation, with ISPB as a tie breaker.

GET /api/v1/br/banks/{code} finds an institution by COMPE code. Accept one to three ASCII digits representing 1 through 999. For example, 1 and 001 identify the same code. Code zero, signs, whitespace, Unicode digits and longer inputs return 400 before any provider call. Unknown valid codes return 404.

The default adapter uses the [BrasilAPI bank directory](https://brasilapi.com.br/docs#tag/BANKS). The source includes institutions with no COMPE code and may represent those using null or zero. Both forms are returned as code: null. Institutions without a COMPE code remain available in the directory. This API exposes directory information only.

Both routes use one immutable directory snapshot cached for 24 hours in process. The snapshot contains at most 1000 records. It is cleared on restart and independent across instances. Provider failures are not cached. Missing bank codes are resolved against that snapshot without extra upstream calls.

The client uses a five-second HTTP timeout, a 2 MiB response buffer limit and redirects disabled. Concurrent cache misses share one outbound slot with a maximum five-second wait. No automatic retries are performed. Existing API rate limits apply.

Provider HTTP failures, empty or malformed responses, invalid identifiers, duplicate ISPB identifiers and ambiguous positive COMPE codes return 502. Provider timeouts and a busy lookup queue return 504. Errors use generic Problem Details and request cancellation propagates to the provider.

Example requests:

```sh
curl http://localhost:5146/api/v1/br/banks
curl http://localhost:5146/api/v1/br/banks/001
```

Example response for a code lookup:

```json
{
  "ispb": "00000000",
  "code": 1,
  "name": "BCO DO BRASIL S.A.",
  "fullName": "Banco do Brasil S.A."
}
```

## Economic indicators (v0.2 development)

GET /api/v1/br/indicators returns the supported catalog without contacting an external provider. Each definition contains code, name, seriesId, unit, observationFrequency, source and sourceUrl.

| Code | BCB SGS series | Value unit | Observation frequency |
| --- | --- | --- | --- |
| selic-target | 432 | Percent per year | Daily |
| cdi | 12 | Percent per day | Daily |
| ipca | 433 | Monthly percentage change | Monthly |

Definitions are based on the official BCB metadata for [Selic target](https://dadosabertos.bcb.gov.br/dataset/432-taxa-de-juros---meta-selic-definida-pelo-copom), [CDI](https://www3.bcb.gov.br/sgspub/consultarvalores/consultarValoresSeries.do?method=consultarSeries&series=12) and [IPCA](https://www3.bcb.gov.br/sgspub/consultarvalores/consultarValoresSeries.do?method=consultarSeries&series=433).

GET /api/v1/br/indicators/{code} returns the latest single record supplied by the BCB SGS endpoint for that series. Codes are case insensitive. Unknown codes return 404 before contacting the provider. An empty source series also returns 404.

The response contains indicator (the catalog definition), value (decimal), referenceDate (ISO date from the source) and retrievedAtUtc (UTC retrieval timestamp). For monthly IPCA, referenceDate is the first day of the reference month, and retrievedAtUtc is the retrieval time rather than a publication date. Cached responses retain the original retrieval timestamp. Reference dates are preserved exactly as supplied, including any dates later than the server's clock.

Values use percentage units. For example, 13.75 in the Selic target series means 13.75% per year. A CDI value of 0.055131 means 0.055131% per day. An IPCA value of -0.12 means a monthly change of -0.12%. Values are parsed with invariant decimal syntax and are not annualized or rounded. These percentage values have a different scale from the fractional ratePerPeriod used by finance calculations.

The default adapter calls https://api.bcb.gov.br/ over HTTPS using a fixed series allowlist. Each successful observation is cached in process for 15 minutes, while missing observations are cached for five minutes. The cache holds at most three entries, is cleared on restart and is independent across instances. Failures are not cached. Cache misses share one outbound slot with a maximum five-second wait. The HTTP client has a five-second timeout, a 32 KiB response buffer limit and redirects disabled. No automatic retries are performed. Existing API rate limits apply.

Malformed data, unparseable decimal values, invalid dates, unexpected multi-record responses and provider HTTP errors return 502. A 304 response without a usable representation is treated as a provider error. Provider timeouts and a busy lookup queue return 504. All errors use generic Problem Details, and request cancellation propagates to the provider.

Example requests:

```sh
curl http://localhost:5146/api/v1/br/indicators
curl http://localhost:5146/api/v1/br/indicators/selic-target
curl http://localhost:5146/api/v1/br/indicators/ipca
```
