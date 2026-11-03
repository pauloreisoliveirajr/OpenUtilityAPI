# API contracts

All POST routes use camelCase JSON. Invalid requests return HTTP 400 with problem details. The rate limiter returns 429. Unexpected failures return 500 with generic problem details.

## Brazilian documents

Under `/api/v1/br/documents`, POST to `cpf/validate`, `cnpj/validate` or `pis/validate` with `{"value":"12ABC34501DE35"}` (CNPJ example). Use ASCII characters in the compact or standard punctuated format. CNPJ accepts ASCII letters in the first twelve positions and requires decimal check digits. Bad syntax returns 400. A check-digit mismatch or repeated-digit sequence returns 200 with `isValid: false` and a reason. Checks are mathematical only.

POST to `format` with `{"type":"cnpj","value":"12abc34501de35"}` to receive `{"type":"cnpj","formatted":"12.ABC.345/01DE-35"}`. Supported types are `cpf` and `cnpj`. Formatting retains leading zeros, uppercases ASCII CNPJ letters and does not validate check digits.

The alphanumeric checksum follows the [Receita Federal technical manual](https://www.gov.br/receitafederal/pt-br/centrais-de-conteudo/publicacoes/documentos-tecnicos/cnpj).
