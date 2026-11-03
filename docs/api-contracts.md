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
