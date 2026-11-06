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
