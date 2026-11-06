import { expect, test, type Page } from '@playwright/test';
import { tools } from '../src/shared/tools.js';

const indicator = {
  code: 'selic-target',
  name: 'Meta Selic',
  seriesId: 432,
  unit: '% per year',
  observationFrequency: 'daily',
  source: 'BCB SGS',
};
async function publicFixtures(page: Page) {
  await page.route('**/api/v1/br/**', async (route) => {
    const path = new URL(route.request().url()).pathname;
    // Document validation remains a real request to ASP.NET.
    if (route.request().method() === 'POST') return route.continue();
    const fixtures: Record<string, unknown> = {
      '/api/v1/br/addresses/01001-000': {
        postalCode: '01001000',
        street: 'Praça da Sé',
        neighborhood: 'Sé',
        complement: '',
        city: 'São Paulo',
        state: 'SP',
        ibgeCode: '3550308',
      },
      '/api/v1/br/localities/states': [
        { id: 53, abbreviation: 'DF', name: 'Distrito Federal', regionName: 'Centro-Oeste' },
      ],
      '/api/v1/br/localities/states/DF/municipalities': [
        { id: 5300108, name: 'Brasília', state: 'DF' },
      ],
      '/api/v1/br/banks': [{ ispb: '00000000', code: 1, name: 'BB', fullName: 'Banco do Brasil' }],
      '/api/v1/br/banks/001': {
        ispb: '00000000',
        code: 1,
        name: 'BB',
        fullName: 'Banco do Brasil',
      },
      '/api/v1/br/indicators': [indicator],
      '/api/v1/br/indicators/selic-target': {
        indicator,
        value: 15,
        referenceDate: '2026-11-05',
        retrievedAtUtc: '2026-11-06T12:00:00Z',
      },
    };
    if (!(path in fixtures)) return route.continue();
    await route.fulfill({ contentType: 'application/json', body: JSON.stringify(fixtures[path]) });
  });
}

for (const language of ['pt-BR', 'en-US']) {
  test(`all 22 tools submit and format results in ${language}`, async ({ page }) => {
    const errors: string[] = [];
    page.on('pageerror', (error) => errors.push(error.message));
    await publicFixtures(page);
    await page.goto('/');
    if (language === 'en-US') {
      await page.getByRole('combobox', { name: 'Idioma', exact: true }).click();
      await page.getByRole('option', { name: 'English' }).click();
    }
    for (const tool of tools) {
      await page.locator(`nav a[href="#${tool.id}"]`).click();
      await expect(page.locator('#form-title')).toHaveText(
        tool.id === 'cep'
          ? language === 'pt-BR'
            ? 'Qual endereço você procura?'
            : 'Which address are you looking for?'
          : language === 'pt-BR'
            ? 'Experimente'
            : 'Try it',
      );
      const pending = page.waitForResponse(
        (r) => r.url().includes('/api/v1/') && r.request().method() === (tool.method ?? 'POST'),
      );
      await page
        .getByRole('button', {
          name: language === 'pt-BR' ? 'Executar chamada' : 'Send request',
          exact: true,
        })
        .click();
      const response = await pending;
      expect(response.status(), tool.id).toBe(200);
      await expect(page.getByText('HTTP 200', { exact: true })).toBeVisible();
      await expect(page.locator('.address-result, .formatted-result')).toBeVisible();
      await page
        .getByRole('button', {
          name: language === 'pt-BR' ? 'Detalhes da chamada' : 'Request details',
        })
        .click();
      await expect(page.locator('pre').last()).toContainText(
        tool.id === 'cep' ? 'postalCode' : '{',
      );
      if (tool.id === 'compound')
        await expect(page.locator('.result-value')).toHaveText(
          language === 'pt-BR' ? '1.126,83' : '1,126.83',
        );
      if (tool.id === 'indicator')
        await expect(page.locator('.result-value')).toContainText(
          language === 'pt-BR' ? '% ao ano' : '% per year',
        );
      await page
        .getByRole('button', {
          name: language === 'pt-BR' ? 'Detalhes da chamada' : 'Request details',
        })
        .click();
    }
    expect(errors).toEqual([]);
  });
}

test('search, language change and browser history preserve the right tool and payload', async ({
  page,
}) => {
  await page.goto('/#words');
  await page.locator('#field-number').fill('123');
  await page.getByRole('button', { name: 'Executar chamada', exact: true }).click();
  await expect(page.locator('.result-value')).toHaveText('cento e vinte e três');
  await page.getByRole('combobox', { name: 'Idioma', exact: true }).click();
  await page.getByRole('option', { name: 'English' }).click();
  await expect(page.locator('#field-number')).toHaveValue('123');
  await expect(page.locator('#field-culture')).toHaveValue('pt-BR');
  await expect(page.locator('.result-value')).toHaveText('cento e vinte e três');
  await page.getByRole('textbox', { name: 'Search tools…' }).fill('interest');
  await expect(page.locator('nav a')).toHaveCount(2);
  await page.locator('nav a[href="#compound"]').click();
  await expect(page).toHaveURL(/#compound$/);
  await page.goBack();
  await expect(page.locator('#field-number')).toBeVisible();
  await page.reload();
  await expect(page.locator('html')).toHaveAttribute('lang', 'en-US');
});

test('client validation, API validation and route/record errors remain distinct', async ({
  page,
}) => {
  await page.goto('/#cep');
  await page.locator('#field-postalCode').fill('123');
  await page.getByRole('button', { name: 'Executar chamada', exact: true }).click();
  await expect(
    page.getByText('Confira o formato e os limites deste campo.', { exact: true }),
  ).toBeVisible();
  await page.locator('#field-postalCode').fill('99999999');
  for (const [title, expected] of [
    ['Postal code not found', 'Nenhum dado encontrado'],
    ['Not Found', 'Rota de consulta indisponível'],
  ]) {
    await page.route('**/api/v1/br/addresses/*', (route) =>
      route.fulfill({
        status: 404,
        contentType: 'application/problem+json',
        body: JSON.stringify({ title, status: 404 }),
      }),
    );
    await page.getByRole('button', { name: 'Executar chamada', exact: true }).click();
    await expect(page.getByText(expected, { exact: true })).toBeVisible();
    await page.unroute('**/api/v1/br/addresses/*');
  }
  await page.goto('/#units');
  await page.locator('#field-toUnit').selectOption('kg');
  await page.getByRole('button', { name: 'Executar chamada', exact: true }).click();
  await expect(page.getByText('HTTP 400', { exact: true })).toBeVisible();
  await expect(page.getByText('Confira os valores informados', { exact: true })).toBeVisible();
});

test('late responses cannot replace the newly selected tool', async ({ page }) => {
  let release!: () => void;
  const gate = new Promise<void>((resolve) => {
    release = resolve;
  });
  await page.route('**/api/v1/br/addresses/*', async (route) => {
    await gate;
    await route.fulfill({ json: { postalCode: '01001000', city: 'São Paulo' } }).catch(() => {});
  });
  await page.goto('/#cep');
  await page.getByRole('button', { name: 'Executar chamada', exact: true }).click();
  await page.locator('nav a[href="#compound"]').click();
  release();
  await page.getByRole('button', { name: 'Executar chamada', exact: true }).click();
  await expect(page.locator('.result-value')).toHaveText('1.126,83');
  await expect(page.locator('.address-result')).toHaveCount(0);
});

test('mobile catalog and table pagination work without page overflow', async ({ page }) => {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/');
  await page.getByRole('button', { name: 'Abrir ferramentas' }).click();
  await page.getByRole('link', { name: 'Financiamento Price', exact: true }).click();
  await page.locator('#field-periods').fill('60');
  await page.getByRole('button', { name: 'Executar chamada', exact: true }).click();
  await expect(page.locator('.data-table tbody tr')).toHaveCount(25);
  await page.getByRole('button', { name: 'Ir para a próxima página' }).click();
  await expect(page.locator('.data-table tbody tr').first().locator('td').first()).toHaveText('26');
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
});
