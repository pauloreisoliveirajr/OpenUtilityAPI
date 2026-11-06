import { tools } from "./tools.js";
import { buildRequest } from "./requests.js";
import { t, getLanguage, setLanguage, restoreLanguage } from "./language.js";

restoreLanguage();

const byId = id => document.getElementById(id);
const form = byId("tool-form");
const fields = byId("fields");
const result = byId("result");
const status = byId("status");
const execute = byId("execute");
const resultPanel = document.querySelector(".result-panel");
let selected;
let activeRequest;
let lastOutcome;

function element(tag, text, className) {
  const node = document.createElement(tag);
  if (text !== undefined) node.textContent = text;
  if (className) node.className = className;
  return node;
}

function renderCatalog() {
  byId("tool-list").replaceChildren();
  const categories = [...new Set(tools.map(tool => tool.category))];
  for (const category of categories) {
    const group = element("div", undefined, "category-group");
    group.append(element("h3", t(category), "category-label"));
    for (const tool of tools.filter(item => item.category === category)) {
      const button = element("button", t(tool.title), "tool-button");
      button.type = "button";
      button.dataset.tool = tool.id;
      button.setAttribute("aria-current", String(selected?.id === tool.id));
      button.addEventListener("click", () => selectTool(tool));
      group.append(button);
    }
    byId("tool-list").append(group);
  }
}

const unitGroups = {
  Comprimento: [["mm", "Milímetro"], ["cm", "Centímetro"], ["m", "Metro"], ["km", "Quilômetro"], ["in", "Polegada"], ["ft", "Pé"], ["mi", "Milha"]],
  Massa: [["g", "Grama"], ["kg", "Quilograma"], ["lb", "Libra"], ["oz", "Onça"]],
  Temperatura: [["C", "Celsius (°C)"], ["F", "Fahrenheit (°F)"], ["K", "Kelvin (K)"]]
};

function createField(field) {
  const label = element("label", undefined, field.type === "checkbox" ? "check-field" : "field");
  label.htmlFor = field.key;
  let input;
  if (field.type === "select" || field.type === "unit") {
    input = element("select");
    if (field.type === "unit") {
      for (const [name, choices] of Object.entries(unitGroups)) {
        const group = element("optgroup");
        group.label = t(name);
        appendOptions(group, choices);
        input.append(group);
      }
    } else appendOptions(input, field.choices);
  } else if (field.type === "textarea" || field.type === "holidays") {
    input = element("textarea");
    input.rows = 3;
  } else {
    input = element("input");
    input.type = field.type;
  }
  input.id = field.key;
  input.name = field.key;
  if (field.type === "checkbox") input.checked = field.value;
  else input.value = field.value;
  input.required = !field.allowEmpty && !["checkbox", "holidays"].includes(field.type);
  if (field.type === "date") { input.min = "0001-01-01"; input.max = "9999-12-31"; }
  for (const key of ["min", "max", "step", "maxLength", "pattern"]) {
    if (field[key] !== undefined) input[key] = field[key];
  }
  if (field.type === "holidays") input.maxLength = 4026;
  if (field.type === "text" || field.type === "textarea") {
    input.autocomplete = "off";
    input.spellcheck = false;
  }
  if (field.type === "checkbox") label.append(input, element("span", t(field.label)));
  else label.append(element("span", t(field.label), "field-title"), input);
  if (field.hint) {
    const hint = element("small", t(field.hint));
    hint.id = field.key + "-hint";
    input.setAttribute("aria-describedby", hint.id);
    label.append(hint);
  }
  return label;
}

function appendOptions(parent, choices) {
  for (const [value, title] of choices) {
    const option = element("option", t(title));
    option.value = value;
    parent.append(option);
  }
}

function renderToolInfo() {
  byId("tool-category").textContent = t(selected.category).toLocaleUpperCase(getLanguage());
  byId("tool-title").textContent = t(selected.title);
  byId("tool-description").textContent = t(selected.description);
  byId("tool-limit").textContent = t(selected.limit);
}

function renderFields() {
  fields.replaceChildren(...selected.fields.map(createField));
  if (selected.fields.length === 0)
    fields.append(element("p", t("Esta consulta não precisa de parâmetros."), "tool-description"));
}

function renderFormError() {
  const error = byId("form-error");
  const field = selected.fields.find(item => item.key === error.dataset.field);
  error.textContent = field
    ? t(field.label) + t(": confira o formato e os limites informados no formulário.")
    : t(error.dataset.message);
  error.hidden = false;
}

function applyLanguage() {
  document.documentElement.lang = getLanguage();
  document.title = t("OpenUtilityAPI · Experimente");
  const description = "Experimente as utilidades da OpenUtilityAPI. Valide documentos, calcule juros e converta valores diretamente no navegador.";
  document.querySelector('meta[name="description"]').content = t(description);
  byId("language-selector").value = getLanguage();
  for (const node of document.querySelectorAll("[data-i18n]")) node.textContent = t(node.dataset.i18n);
  for (const node of document.querySelectorAll("[data-i18n-label]")) node.setAttribute("aria-label", t(node.dataset.i18nLabel));
  for (const node of document.querySelectorAll("[data-tool-count]")) node.textContent = tools.length;
  renderCatalog();
  renderToolInfo();
  // Keep raw field values and response data when changing interface language.
  const values = new Map(Array.from(form.elements).map(input => [input.name, { value: input.value, checked: input.checked }]));
  renderFields();
  for (const field of selected.fields) {
    const input = form.elements.namedItem(field.key);
    const previous = values.get(field.key);
    if (field.type === "checkbox") input.checked = previous.checked;
    else input.value = previous.value;
  }
  if (!byId("form-error").hidden) renderFormError();
  updateRequest();
  renderOutcome();
}

function renderOutcome() {
  if (lastOutcome?.message) {
    status.textContent = t("Sem resposta");
    status.className = "status-label error";
    result.replaceChildren(element("p", t(lastOutcome.message), "result-error"));
    byId("response-json").textContent = t("Nenhuma resposta recebida.");
  } else if (lastOutcome) {
    status.textContent = "HTTP " + lastOutcome.status;
    status.className = lastOutcome.ok ? "status-label" : "status-label error";
    if (lastOutcome.ok) renderSuccess(lastOutcome.data);
    else renderError(lastOutcome.data, lastOutcome.status, lastOutcome.retryAfter);
  } else if (activeRequest) {
    status.textContent = t("Executando…");
    byId("response-json").textContent = t("Aguardando resposta…");
    result.replaceChildren(element("p", t("Consultando a API…"), "empty-state"));
  } else {
    status.textContent = t("Aguardando");
    byId("response-json").textContent = t("Execute uma chamada para ver a resposta.");
    const empty = element("div", undefined, "empty-state");
    empty.append(element("span", "⌁"), element("strong", t("Sua resposta aparece aqui")),
      element("p", t("Preencha os campos e execute uma chamada para começar.")));
    result.replaceChildren(empty);
  }
}

function selectTool(tool) {
  activeRequest?.abort();
  activeRequest = null;
  selected = tool;
  lastOutcome = null;
  for (const button of document.querySelectorAll("[data-tool]")) {
    button.setAttribute("aria-current", String(button.dataset.tool === tool.id));
  }
  renderToolInfo();
  byId("method").textContent = tool.method ?? "POST";
  byId("request-method").textContent = tool.method ?? "POST";
  renderFields();
  byId("form-error").hidden = true;
  byId("response-json").textContent = t("Execute uma chamada para ver a resposta.");
  status.textContent = t("Aguardando");
  status.className = "status-label";
  resultPanel.setAttribute("aria-busy", "false");
  execute.disabled = false;
  result.replaceChildren();
  renderOutcome();
  updateRequest();
}

function payload() {
  const values = {};
  for (const field of selected.fields) {
    const input = form.elements.namedItem(field.key);
    if (field.type === "checkbox") values[field.key] = input.checked;
    else if (field.type === "number") {
      const number = input.valueAsNumber;
      if (!Number.isFinite(number)) throw new Error("Preencha os campos numéricos com valores válidos.");
      values[field.key] = number;
    } else if (field.type === "holidays") {
      const dates = input.value.split(/\r?\n/).map(value => value.trim()).filter(Boolean);
      if (dates.length > 366 || dates.some(date => !/^\d{4}-\d{2}-\d{2}$/.test(date))) {
        throw new Error("Informe até 366 feriados, um por linha, no formato AAAA-MM-DD.");
      }
      values[field.key] = dates;
    } else values[field.key] = input.value;
  }
  return values;
}

function updateRequest() {
  try {
    const request = buildRequest(selected, payload());
    byId("endpoint").textContent = request.url;
    byId("request-json").textContent = JSON.stringify(request, null, 2);
  } catch {
    byId("endpoint").textContent = "/api/v1/" + selected.path;
    byId("request-json").textContent = t("Complete os campos para gerar a requisição.");
  }
}
form.addEventListener("input", updateRequest);
byId("reset").addEventListener("click", () => selectTool(selected));

function number(value) { return typeof value === "number" ? new Intl.NumberFormat(getLanguage(), { maximumFractionDigits: 12 }).format(value) : String(value); }
function date(value) { return new Intl.DateTimeFormat(getLanguage(), { timeZone: "UTC" }).format(new Date(value + "T00:00:00Z")); }
function mainResult(label, value, note) {
  const main = element("div", undefined, "result-main");
  main.append(element("small", label), element("strong", value));
  if (note) main.append(element("p", note));
  result.append(main);
}
function row(label, value) {
  const line = element("div", undefined, "result-row");
  line.append(element("span", label), element("strong", value));
  result.append(line);
}

function lookupTable(headers, rows) {
  const scroll = element("div", undefined, "table-scroll");
  scroll.tabIndex = 0;
  scroll.setAttribute("aria-label", t("Dados da consulta"));
  const table = element("table");
  const head = element("thead");
  const heading = element("tr");
  for (const title of headers) {
    const cell = element("th", t(title));
    cell.scope = "col";
    heading.append(cell);
  }
  head.append(heading);
  const body = element("tbody");
  for (const values of rows) {
    const line = element("tr");
    for (const value of values) line.append(element("td", value));
    body.append(line);
  }
  table.append(head, body);
  scroll.append(table);
  result.append(scroll);
}

function compe(code) { return code === null ? t("Sem COMPE") : String(code).padStart(3, "0"); }
function indicatorName(indicator) {
  const names = { "selic-target": "Meta Selic", cdi: "CDI diário", ipca: "IPCA mensal" };
  return t(names[indicator.code] ?? indicator.name);
}
function indicatorUnit(unit) {
  const units = { "% per year": "% ao ano", "% per day": "% ao dia", "monthly % change": "% de variação mensal" };
  return t(units[unit] ?? unit);
}
function indicatorFrequency(frequency) { return frequency === "daily" ? t("Diária") : t("Mensal"); }

function renderSuccess(data) {
  result.replaceChildren();
  if (Array.isArray(data)) {
    mainResult(t("Registros encontrados"), number(data.length));
    if (selected.id === "states")
      lookupTable(["Código IBGE", "Sigla da UF", "Nome", "Região"], data.map(item => [String(item.id), item.abbreviation, item.name, item.regionName]));
    else if (selected.id === "municipalities")
      lookupTable(["Código IBGE", "Nome", "Sigla da UF"], data.map(item => [String(item.id), item.name, item.state]));
    else if (selected.id === "banks")
      lookupTable(["ISPB", "COMPE", "Nome", "Nome completo"], data.map(item => [item.ispb, compe(item.code), item.name, item.fullName]));
    else if (selected.id === "indicator-catalog")
      lookupTable(["Indicador", "Série SGS", "Unidade", "Frequência"], data.map(item => [indicatorName(item), String(item.seriesId), indicatorUnit(item.unit), indicatorFrequency(item.observationFrequency)]));
  } else if (selected.id === "cep") {
    mainResult(t("Endereço encontrado"), data.postalCode.slice(0, 5) + "-" + data.postalCode.slice(5));
    row(t("Logradouro"), data.street || t("Não informado"));
    row(t("Complemento"), data.complement || t("Não informado"));
    row(t("Bairro"), data.neighborhood || t("Não informado"));
    row(t("Cidade"), data.city);
    row(t("Sigla da UF"), data.state);
    row(t("Código IBGE"), data.ibgeCode);
  } else if (selected.id === "bank") {
    mainResult(t("Instituição encontrada"), data.name);
    row(t("Nome completo"), data.fullName);
    row("ISPB", data.ispb);
    row("COMPE", compe(data.code));
  } else if (selected.id === "indicator") {
    mainResult(indicatorName(data.indicator), number(data.value) + " " + indicatorUnit(data.indicator.unit));
    const reference = data.indicator.observationFrequency === "monthly"
      ? new Intl.DateTimeFormat(getLanguage(), { month: "long", year: "numeric", timeZone: "UTC" }).format(new Date(data.referenceDate + "T00:00:00Z"))
      : date(data.referenceDate);
    row(t("Referência"), reference);
    row(t("Frequência"), indicatorFrequency(data.indicator.observationFrequency));
    row(t("Fonte"), data.indicator.source);
    row(t("Série SGS"), String(data.indicator.seriesId));
    row(t("Consulta (UTC)"), new Intl.DateTimeFormat(getLanguage(), { dateStyle: "short", timeStyle: "short", timeZone: "UTC" }).format(new Date(data.retrievedAtUtc)));
  } else if ("isValid" in data) {
    mainResult(selected.id === "email" ? t("Verificação de sintaxe") : t("Verificação matemática"),
      data.isValid ? t("Válido") : t("Inválido"),
      data.isValid ? t("O exemplo passou nas verificações desta ferramenta.") : t("O valor não passou nas verificações desta ferramenta."));
  } else if ("finalAmount" in data) {
    mainResult(t("Montante final"), number(data.finalAmount), t("Na mesma unidade monetária do valor inicial."));
    row(t("Juros"), number(data.interestAmount));
    row(t("Valor inicial"), number(data.principal));
    row(t("Taxa por período"), number(data.ratePerPeriod * 100) + "%");
    row(t("Períodos"), number(data.periods));
  } else if ("installments" in data) {
    mainResult(t("Total a pagar"), number(data.totalPayment), t("Na mesma unidade monetária do valor inicial."));
    row(t("Total de juros"), number(data.totalInterest));
    row(t("Parcelas"), number(data.periods));
    const scroll = element("div", undefined, "table-scroll");
    scroll.tabIndex = 0;
    scroll.setAttribute("aria-label", t("Tabela de parcelas"));
    const table = element("table");
    const head = element("thead");
    const header = element("tr");
    for (const title of [t("Parcela"), t("Amortização"), t("Juros"), t("Pagamento"), t("Saldo")]) {
      const th = element("th", t(title));
      th.scope = "col";
      header.append(th);
    }
    head.append(header);
    const body = element("tbody");
    for (const installment of data.installments) {
      const tr = element("tr");
      for (const key of ["period", "amortization", "interest", "payment", "balance"]) tr.append(element("td", number(installment[key])));
      body.append(tr);
    }
    table.append(head, body);
    scroll.append(table);
    result.append(scroll);
  } else if ("formatted" in data) mainResult(t("Documento formatado"), data.formatted);
  else if ("words" in data) mainResult(t("Número por extenso"), data.words);
  else if ("text" in data) mainResult(t("Texto normalizado"), data.text || t("(texto vazio)"));
  else if (selected.id === "difference") mainResult(t("Diferença"), number(data.days) + t(" dias"));
  else if (selected.id === "business") mainResult(t("Data resultante"), date(data.result));
  else mainResult(t("Resultado"), number(data.result) + (data.toUnit ? " " + data.toUnit : data.unit === "%" ? "%" : ""));
  if (data.calculation) {
    row(t("Arredondamento"), t("Meio para longe de zero"));
    row(t("Casas decimais"), number(data.calculation.decimalPlaces));
  }
}
function renderError(data, httpStatus, retryAfter) {
  result.replaceChildren();
  const block = element("div", undefined, "result-error");
  let message = t("Não foi possível concluir a chamada. Confira os campos e tente novamente.");
  if (httpStatus === 429) {
    const seconds = Number(retryAfter);
    message = Number.isFinite(seconds) && seconds > 0
      ? t("Limite de chamadas atingido. Tente novamente em ") + Math.ceil(seconds) + t(" segundos.")
      : t("Limite de chamadas atingido. Aguarde um momento e tente novamente.");
  } else if (httpStatus === 404) message = t("Nenhum dado encontrado para esta consulta.");
  else if (httpStatus === 502 || httpStatus === 504) message = t("A fonte externa está indisponível. Tente novamente em instantes.");
  else if (httpStatus >= 500) message = t("O serviço encontrou um erro. Tente novamente em instantes.");
  block.append(element("strong", message));
  if (httpStatus === 400 && data.errors) {
    const list = element("ul");
    if (selected.id === "units") {
      list.append(element("li", t("Confira o valor e escolha unidades da mesma categoria. Temperaturas devem respeitar o zero absoluto.")));
    } else {
      for (const key of Object.keys(data.errors)) {
        const name = selected.fields.find(field => field.key.toLowerCase() === key.toLowerCase())?.label ?? "Requisição";
        list.append(element("li", t(name) + t(": confira o formato e os limites informados no formulário.")));
      }
    }
    block.append(list);
  }
  result.append(block);
}

form.addEventListener("submit", async event => {
  event.preventDefault();
  if (execute.disabled) return;
  const error = byId("form-error");
  error.hidden = true;
  error.dataset.field = "";
  if (!form.checkValidity()) {
    const invalid = form.querySelector(":invalid");
    error.dataset.field = invalid.name;
    renderFormError();
    invalid.focus();
    return;
  }
  let request;
  try { request = buildRequest(selected, payload()); }
  catch (failure) { error.dataset.message = failure.message; error.textContent = t(failure.message); error.hidden = false; return; }
  lastOutcome = null;
  const controller = new AbortController();
  activeRequest = controller;
  execute.disabled = true;
  resultPanel.setAttribute("aria-busy", "true");
  status.className = "status-label";
  status.textContent = t("Executando…");
  byId("endpoint").textContent = request.url;
  byId("request-json").textContent = JSON.stringify(request, null, 2);
  byId("response-json").textContent = t("Aguardando resposta…");
  result.replaceChildren(element("p", t("Consultando a API…"), "empty-state"));
  const timeout = setTimeout(() => controller.abort("timeout"), 20000);
  try {
    const response = await fetch(request.url, {
      method: request.method,
      ...(request.method === "POST" ? { headers: { "Content-Type": "application/json" }, body: JSON.stringify(request.body) } : {}),
      signal: controller.signal
    });
    const text = await response.text();
    if (activeRequest !== controller) return;
    let data;
    try { data = JSON.parse(text); } catch { data = {}; }
    byId("response-json").textContent = Object.keys(data).length ? JSON.stringify(data, null, 2) : text;
    lastOutcome = { data, status: response.status, ok: response.ok, retryAfter: response.headers.get("Retry-After") };
    renderOutcome();
  } catch {
    if (activeRequest !== controller) return;
    lastOutcome = { message: controller.signal.aborted
      ? "A chamada excedeu o tempo de espera. Tente novamente."
      : "Não foi possível acessar a API. Verifique a conexão e tente novamente." };
    renderOutcome();
  } finally {
    clearTimeout(timeout);
    if (activeRequest === controller) {
      activeRequest = null;
      execute.disabled = false;
      resultPanel.setAttribute("aria-busy", "false");
    }
  }
});

selectTool(tools[0]);
applyLanguage();
byId("language-selector").addEventListener("change", event => {
  setLanguage(event.target.value);
  applyLanguage();
});
