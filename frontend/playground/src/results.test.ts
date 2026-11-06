import { expect, it } from 'vitest';
import { tools } from './catalog';
import { present } from './results';
const tool = (id: string) => tools.find((t) => t.id === id)!;
it('formats amounts in the selected locale without inventing a currency', () => {
  const data = {
    finalAmount: 1126.83,
    interestAmount: 126.83,
    principal: 1000,
    ratePerPeriod: 0.01,
    periods: 12,
    calculation: { decimalPlaces: 2 },
  };
  expect(present(tool('compound'), data, 'pt-BR').value).toBe('1.126,83');
  expect(present(tool('compound'), data, 'en-US').value).toBe('1,126.83');
  expect(present(tool('compound'), data, 'en-US').rows).toContainEqual(['Rate per period', '1%']);
});
it('preserves indicator units and the source reference date', () => {
  const data = {
    indicator: {
      code: 'cdi',
      unit: '% per day',
      observationFrequency: 'daily',
      seriesId: 12,
      source: 'BCB SGS',
    },
    value: 0.05,
    referenceDate: '2026-11-05',
    retrievedAtUtc: '2026-11-06T12:00:00Z',
  };
  const model = present(tool('indicator'), data, 'en-US');
  expect(model.value).toBe('0.05 % per day');
  expect(model.rows).toContainEqual(['Reference', '11/5/2026']);
  expect(model.rows).toContainEqual(['SGS series', '12']);
});
it('preserves all installment rows and codes with leading zeros', () => {
  const installments = Array.from({ length: 360 }, (_, i) => ({
    period: i + 1,
    amortization: 1,
    interest: 2,
    payment: 3,
    balance: 4,
  }));
  expect(present(tool('price'), { installments }, 'pt-BR').table?.rows).toHaveLength(360);
  expect(
    present(
      tool('banks'),
      [{ ispb: '00000000', code: 1, name: 'BB', fullName: 'Banco do Brasil' }],
      'en-US',
    ).table?.rows[0],
  ).toEqual(['00000000', '001', 'BB', 'Banco do Brasil']);
});
it('renders negative validations as results and keeps chosen output language unchanged', () => {
  expect(present(tool('cpf'), { isValid: false }, 'pt-BR')).toMatchObject({
    valid: false,
    value: 'Inválido',
  });
  expect(present(tool('words'), { words: 'quarenta e dois' }, 'en-US').value).toBe(
    'quarenta e dois',
  );
});
