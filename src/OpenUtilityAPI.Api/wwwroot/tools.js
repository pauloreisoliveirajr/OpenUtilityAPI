const field = (key, label, type, value, options = {}) => ({ key, label, type, value, ...options });
const documentField = value => field("value", "Documento", "text", value, { maxLength: 18 });
const precision = maximum => field("decimalPlaces", "Casas decimais", "number", 2, { min: 0, max: maximum, step: 1 });
const financeFields = minimumPeriods => [
  field("principal", "Valor inicial", "number", 1000, { min: 0, max: 1e12, step: "any" }),
  field("ratePerPeriod", "Taxa por período", "number", 0.01, { min: 0, max: 0.05, step: "any", hint: "Use a taxa como fração: 0,01 equivale a 1% por período." }),
  field("periods", "Número de períodos", "number", 12, { min: minimumPeriods, max: 360, step: 1 }),
  precision(6)
];
const startDate = () => field("start", "Data inicial", "date", "2024-03-01");
const financeLimit = "Taxa de 0 a 5% por período, até 360 períodos. Arredondamento de meio para longe de zero. Valores sem tarifas ou impostos.";
const documentDescription = "Confira o formato e os dígitos verificadores usando um exemplo. A validação é matemática e não consulta situação cadastral.";
export const tools = [
  { id: "cpf", category: "Documentos brasileiros", title: "Validar CPF", path: "br/documents/cpf/validate", description: documentDescription, fields: [documentField("529.982.247-25")], limit: "Aceita 11 dígitos, com ou sem a máscara padrão." },
  { id: "cnpj", category: "Documentos brasileiros", title: "Validar CNPJ", path: "br/documents/cnpj/validate", description: documentDescription, fields: [documentField("12.ABC.345/01DE-35")], limit: "Aceita CNPJ numérico e alfanumérico. Os dois dígitos verificadores são numéricos." },
  { id: "format", category: "Documentos brasileiros", title: "Formatar documento", path: "br/documents/format", description: "Aplique a máscara de CPF ou CNPJ preservando os zeros iniciais. As letras do CNPJ são convertidas para maiúsculas.", fields: [
    field("type", "Tipo de documento", "select", "cnpj", { choices: [["cpf", "CPF"], ["cnpj", "CNPJ"]] }), documentField("12abc34501de35")
  ], limit: "A formatação não valida os dígitos verificadores." },
  { id: "pis", category: "Documentos brasileiros", title: "Validar PIS/PASEP", path: "br/documents/pis/validate", description: documentDescription, fields: [documentField("120.44558.95-7")], limit: "Aceita 11 dígitos, com ou sem a máscara padrão." },
  { id: "simple", category: "Finanças", title: "Juros simples", path: "finance/simple-interest", description: "Calcule o montante e os juros com uma taxa constante aplicada ao valor inicial em cada período.", fields: financeFields(0), limit: financeLimit },
  { id: "compound", category: "Finanças", title: "Juros compostos", path: "finance/compound-interest", description: "Veja como o valor inicial cresce quando os juros de cada período são incorporados ao saldo.", fields: financeFields(0), limit: financeLimit },
  { id: "price", category: "Finanças", title: "Financiamento Price", path: "finance/price", description: "Simule o financiamento pelo sistema Price e acompanhe a amortização, os juros e o saldo de cada parcela.", fields: financeFields(1), limit: financeLimit + " O valor inicial deve caber na precisão escolhida." },
  { id: "sac", category: "Finanças", title: "Financiamento SAC", path: "finance/sac", description: "Simule parcelas com amortização constante e juros calculados sobre o saldo devedor.", fields: financeFields(1), limit: financeLimit + " O valor inicial deve caber na precisão escolhida." },
  { id: "percentage", category: "Matemática", title: "Porcentagem", path: "math/percentage", description: "Calcule uma porcentagem, a proporção entre valores ou a variação percentual.", fields: [
    field("operation", "Operação", "select", "percentOf", { choices: [["percentOf", "Porcentagem de um valor"], ["ratioPercent", "Proporção percentual"], ["changePercent", "Variação percentual"]] }),
    field("value", "Valor de referência", "number", 200, { min: -1e12, max: 1e12, step: "any" }),
    field("operand", "Percentual ou segundo valor", "number", 15, { min: -1e12, max: 1e12, step: "any", hint: "Porcentagem: 15 significa 15%. Proporção: referência ÷ segundo valor × 100. Variação: da referência ao segundo valor." }),
    precision(6)
  ], limit: "Valores entre −10¹² e 10¹². Operações com divisor zero são rejeitadas." },
  { id: "units", category: "Conversões", title: "Converter unidades", path: "converters/units", description: "Converta comprimento, massa ou temperatura. Escolha duas unidades da mesma categoria.", fields: [
    field("value", "Valor", "number", 32, { min: -1e12, max: 1e12, step: "any" }),
    field("fromUnit", "De", "unit", "F"), field("toUnit", "Para", "unit", "C"),
    { ...precision(12), value: 6 }
  ], limit: "Comprimento: mm, cm, m, km, in, ft e mi. Massa: g, kg, lb e oz. Temperatura: °C, °F e K. Valores abaixo do zero absoluto são rejeitados." },
  { id: "words", category: "Conversões", title: "Número por extenso", path: "converters/number-to-words", description: "Transforme um número inteiro em palavras em português ou inglês.", fields: [
    field("number", "Número inteiro", "number", 42, { min: -999999, max: 999999, step: 1 }),
    field("culture", "Idioma do resultado", "select", "pt-BR", { choices: [["pt-BR", "Português do Brasil"], ["en-US", "Inglês dos EUA"]] })
  ], limit: "Intervalo de −999999 a 999999. Apenas números inteiros." },
  { id: "difference", category: "Datas", title: "Diferença entre datas", path: "datetime/difference", description: "Conte os dias corridos entre duas datas do calendário, incluindo anos bissextos.", fields: [startDate(), field("end", "Data final", "date", "2024-03-15")], limit: "Resultado = data final − data inicial. Datas iguais retornam zero; datas anteriores retornam um valor negativo." },
  { id: "business", category: "Datas", title: "Adicionar dias úteis", path: "datetime/add-business-days", description: "Avance ou retroceda no calendário pulando sábados, domingos e os feriados que você informar.", fields: [
    startDate(), field("days", "Quantidade de dias úteis", "number", 5, { min: -10000, max: 10000, step: 1 }),
    field("holidays", "Feriados (opcional)", "holidays", "", { hint: "Uma data por linha no formato AAAA-MM-DD. Exemplo: 2024-03-04." })
  ], limit: "A data inicial não entra na contagem. Zero dias mantém a data original. Até 10000 dias em cada direção e 366 feriados." },
  { id: "normalize", category: "Texto", title: "Normalizar texto", path: "text/normalize", description: "Ajuste espaços, acentos e letras maiúsculas ou minúsculas de acordo com as opções selecionadas.", fields: [
    field("text", "Texto", "textarea", " Café   com leite ", { allowEmpty: true, maxLength: 10000 }),
    field("trim", "Remover espaços nas extremidades", "checkbox", true),
    field("collapseWhitespace", "Reduzir espaços repetidos", "checkbox", true),
    field("removeAccents", "Remover acentos", "checkbox", true),
    field("letterCase", "Maiúsculas e minúsculas", "select", "upper", { choices: [["preserve", "Manter como está"], ["lower", "minúsculas"], ["upper", "MAIÚSCULAS"]] })
  ], limit: "Até 10000 caracteres UTF-16. As opções são aplicadas na ordem exibida." },
  { id: "email", category: "Texto", title: "Validar e-mail", path: "text/email/validate", description: "Verifique se o endereço segue o perfil de sintaxe ASCII aceito pela API.", fields: [field("value", "Endereço de e-mail", "text", "pessoa@example.com", { maxLength: 254 })], limit: "Somente sintaxe: não verifica existência ou entrega. Endereços Unicode e nomes de exibição não são suportados." }
];
