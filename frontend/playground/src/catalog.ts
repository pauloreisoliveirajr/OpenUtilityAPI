import { tools as definitions } from './shared/tools.js';

export interface Field {
  key: string;
  label: string;
  type: 'text' | 'number' | 'date' | 'select' | 'unit' | 'holidays' | 'textarea' | 'checkbox';
  value: string | number | boolean;
  choices?: [string, string][];
  hint?: string;
  min?: number;
  max?: number;
  step?: number | 'any';
  maxLength?: number;
  pattern?: string;
  allowEmpty?: boolean;
}
export interface Tool {
  id: string;
  category: string;
  title: string;
  description: string;
  limit: string;
  path: string;
  method?: 'GET' | 'POST';
  fields: Field[];
}
export const tools: Tool[] = definitions as Tool[];
export type Values = Record<string, string | boolean>;
export const initialValues = (tool: Tool): Values =>
  Object.fromEntries(
    tool.fields.map((field) => [
      field.key,
      field.type === 'checkbox' ? Boolean(field.value) : String(field.value),
    ]),
  );
export const unitGroups: Record<string, [string, string][]> = {
  Comprimento: [
    ['mm', 'Milímetro'],
    ['cm', 'Centímetro'],
    ['m', 'Metro'],
    ['km', 'Quilômetro'],
    ['in', 'Polegada'],
    ['ft', 'Pé'],
    ['mi', 'Milha'],
  ],
  Massa: [
    ['g', 'Grama'],
    ['kg', 'Quilograma'],
    ['lb', 'Libra'],
    ['oz', 'Onça'],
  ],
  Temperatura: [
    ['C', 'Celsius (°C)'],
    ['F', 'Fahrenheit (°F)'],
    ['K', 'Kelvin (K)'],
  ],
};
export class FieldError extends Error {
  constructor(
    public field: string,
    public messageKey: string,
  ) {
    super(messageKey);
  }
}
export function payload(tool: Tool, values: Values): Record<string, unknown> {
  return Object.fromEntries(
    tool.fields.map((field) => {
      const value = values[field.key];
      if (field.type === 'checkbox') return [field.key, Boolean(value)];
      if (field.type === 'number') {
        if (String(value).trim() === '' || !Number.isFinite(Number(value)))
          throw new FieldError(field.key, 'Preencha os campos numéricos com valores válidos.');
        return [field.key, Number(value)];
      }
      if (field.type === 'holidays') {
        const dates = String(value)
          .split(/\r?\n/)
          .map((date) => date.trim())
          .filter(Boolean);
        if (dates.length > 366 || dates.some((date) => !/^\d{4}-\d{2}-\d{2}$/.test(date)))
          throw new FieldError(
            field.key,
            'Informe até 366 feriados, um por linha, no formato AAAA-MM-DD.',
          );
        return [field.key, dates];
      }
      return [field.key, value];
    }),
  );
}
