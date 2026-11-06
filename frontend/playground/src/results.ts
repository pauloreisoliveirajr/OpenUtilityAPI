import type { Tool } from './catalog';
import type { Language } from './messages';
import { t as translate } from './shared/language.js';

export interface ResultModel {
  heading: string;
  value: string;
  note?: string;
  valid?: boolean;
  rows?: [string, string][];
  table?: { headers: string[]; rows: string[][] };
}
export const record = (value: unknown): Record<string, unknown> =>
  value && typeof value === 'object' && !Array.isArray(value)
    ? (value as Record<string, unknown>)
    : {};
const text = (value: unknown) => (value === undefined || value === null ? '' : String(value));

export function present(tool: Tool, data: unknown, language: Language): ResultModel {
  const t = (key: string) => translate(key, language);
  const n = (value: unknown) =>
    typeof value === 'number'
      ? new Intl.NumberFormat(language, { maximumFractionDigits: 12 }).format(value)
      : text(value);
  const date = (value: unknown, monthly = false) => {
    const raw = text(value);
    const parsed = new Date(raw + 'T00:00:00Z');
    return Number.isNaN(parsed.getTime())
      ? raw
      : new Intl.DateTimeFormat(
          language,
          monthly ? { month: 'long', year: 'numeric', timeZone: 'UTC' } : { timeZone: 'UTC' },
        ).format(parsed);
  };
  const compe = (value: unknown) =>
    value === null ? t('Sem COMPE') : text(value).padStart(3, '0');
  const indicatorName = (value: unknown) => {
    const indicator = record(value);
    return t(
      (
        { 'selic-target': 'Meta Selic', cdi: 'CDI diário', ipca: 'IPCA mensal' } as Record<
          string,
          string
        >
      )[text(indicator.code)] ?? text(indicator.name),
    );
  };
  const unit = (value: unknown) =>
    t(
      (
        {
          '% per year': '% ao ano',
          '% per day': '% ao dia',
          'monthly % change': '% de variação mensal',
        } as Record<string, string>
      )[text(value)] ?? text(value),
    );
  const frequency = (value: unknown) => t(value === 'daily' ? 'Diária' : 'Mensal');
  if (Array.isArray(data)) {
    const items = data.map(record);
    let headers: string[] = [],
      rows: string[][] = [];
    if (tool.id === 'states') {
      headers = ['Código IBGE', 'Sigla da UF', 'Nome', 'Região'];
      rows = items.map((i) => [text(i.id), text(i.abbreviation), text(i.name), text(i.regionName)]);
    }
    if (tool.id === 'municipalities') {
      headers = ['Código IBGE', 'Nome', 'Sigla da UF'];
      rows = items.map((i) => [text(i.id), text(i.name), text(i.state)]);
    }
    if (tool.id === 'banks') {
      headers = ['ISPB', 'COMPE', 'Nome', 'Nome completo'];
      rows = items.map((i) => [text(i.ispb), compe(i.code), text(i.name), text(i.fullName)]);
    }
    if (tool.id === 'indicator-catalog') {
      headers = ['Indicador', 'Série SGS', 'Unidade', 'Frequência'];
      rows = items.map((i) => [
        indicatorName(i),
        text(i.seriesId),
        unit(i.unit),
        frequency(i.observationFrequency),
      ]);
    }
    return {
      heading: t('Registros encontrados'),
      value: n(data.length),
      table: { headers: headers.map(t), rows },
    };
  }
  const d = record(data);
  let model: ResultModel;
  if (tool.id === 'bank')
    model = {
      heading: t('Instituição encontrada'),
      value: text(d.name),
      rows: [
        [t('Nome completo'), text(d.fullName)],
        ['ISPB', text(d.ispb)],
        ['COMPE', compe(d.code)],
      ],
    };
  else if (tool.id === 'indicator') {
    const indicator = record(d.indicator);
    const retrieved = new Date(text(d.retrievedAtUtc));
    model = {
      heading: indicatorName(indicator),
      value: `${n(d.value)} ${unit(indicator.unit)}`,
      rows: [
        [t('Referência'), date(d.referenceDate, indicator.observationFrequency === 'monthly')],
        [t('Frequência'), frequency(indicator.observationFrequency)],
        [t('Fonte'), text(indicator.source)],
        [t('Série SGS'), text(indicator.seriesId)],
        [
          t('Consulta (UTC)'),
          Number.isNaN(retrieved.getTime())
            ? text(d.retrievedAtUtc)
            : new Intl.DateTimeFormat(language, {
                dateStyle: 'short',
                timeStyle: 'short',
                timeZone: 'UTC',
              }).format(retrieved),
        ],
      ],
    };
  } else if ('isValid' in d)
    model = {
      heading: t(tool.id === 'email' ? 'Verificação de sintaxe' : 'Verificação matemática'),
      value: t(d.isValid ? 'Válido' : 'Inválido'),
      valid: d.isValid === true,
      note: t(
        d.isValid
          ? 'O exemplo passou nas verificações desta ferramenta.'
          : 'O valor não passou nas verificações desta ferramenta.',
      ),
    };
  else if ('finalAmount' in d)
    model = {
      heading: t('Montante final'),
      value: n(d.finalAmount),
      note: t('Na mesma unidade monetária do valor inicial.'),
      rows: [
        [t('Juros'), n(d.interestAmount)],
        [t('Valor inicial'), n(d.principal)],
        [t('Taxa por período'), n(Number(d.ratePerPeriod) * 100) + '%'],
        [t('Períodos'), n(d.periods)],
      ],
    };
  else if (Array.isArray(d.installments))
    model = {
      heading: t('Total a pagar'),
      value: n(d.totalPayment),
      note: t('Na mesma unidade monetária do valor inicial.'),
      rows: [
        [t('Total de juros'), n(d.totalInterest)],
        [t('Parcelas'), n(d.periods)],
      ],
      table: {
        headers: ['Parcela', 'Amortização', 'Juros', 'Pagamento', 'Saldo'].map(t),
        rows: d.installments
          .map(record)
          .map((i) =>
            ['period', 'amortization', 'interest', 'payment', 'balance'].map((key) => n(i[key])),
          ),
      },
    };
  else if ('formatted' in d)
    model = { heading: t('Documento formatado'), value: text(d.formatted) };
  else if ('words' in d) model = { heading: t('Número por extenso'), value: text(d.words) };
  else if ('text' in d)
    model = { heading: t('Texto normalizado'), value: text(d.text) || t('(texto vazio)') };
  else if (tool.id === 'difference')
    model = { heading: t('Diferença'), value: n(d.days) + t(' dias') };
  else if (tool.id === 'business') model = { heading: t('Data resultante'), value: date(d.result) };
  else
    model = {
      heading: t('Resultado'),
      value: n(d.result) + (d.toUnit ? ' ' + text(d.toUnit) : d.unit === '%' ? '%' : ''),
    };
  if (d.calculation) {
    model.rows = [
      ...(model.rows ?? []),
      [t('Arredondamento'), t('Meio para longe de zero')],
      [t('Casas decimais'), n(record(d.calculation).decimalPlaces)],
    ];
  }
  return model;
}
