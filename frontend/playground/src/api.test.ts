import { afterEach, expect, it, vi } from 'vitest';
import { execute } from './api';
import { initialValues, payload, tools } from './catalog';
import { buildRequest } from './shared/requests.js';
afterEach(() => vi.unstubAllGlobals());
it('preserves all 22 contracts, including numbers, booleans and holidays', () => {
  expect(tools).toHaveLength(22);
  for (const tool of tools) {
    const values = payload(tool, initialValues(tool));
    const request = buildRequest(tool, values);
    expect(request.url).not.toContain('{');
    expect(request.method).toBe(tool.method ?? 'POST');
    for (const field of tool.fields) {
      if (field.type === 'number') expect(typeof values[field.key]).toBe('number');
      if (field.type === 'checkbox') expect(typeof values[field.key]).toBe('boolean');
      if (field.type === 'holidays') expect(values[field.key]).toEqual([]);
    }
  }
});
it('rejects blank numbers and excessive or malformed holidays', () => {
  const finance = tools.find((t) => t.id === 'simple')!;
  expect(() => payload(finance, { ...initialValues(finance), principal: '' })).toThrow();
  const business = tools.find((t) => t.id === 'business')!;
  for (const holidays of ['04/03/2024', Array(367).fill('2024-03-04').join('\n')])
    expect(() => payload(business, { ...initialValues(business), holidays })).toThrow();
  expect(
    payload(business, { ...initialValues(business), holidays: '2024-03-04\r\n\n2024-03-05' })
      .holidays,
  ).toEqual(['2024-03-04', '2024-03-05']);
});
it('sends GET without body and POST with JSON and cancellation', async () => {
  const fetch = vi.fn().mockImplementation(() => Promise.resolve(new Response('{"result":1}')));
  vi.stubGlobal('fetch', fetch);
  const signal = new AbortController().signal;
  await execute({ method: 'GET', url: '/api/v1/br/banks/001' }, signal);
  expect(fetch.mock.calls[0][1]).not.toHaveProperty('body');
  await execute({ method: 'POST', url: '/api/v1/math/percentage', body: { value: 200 } }, signal);
  expect(fetch.mock.calls[1][1]).toMatchObject({
    method: 'POST',
    body: '{"value":200}',
    signal,
    headers: { 'Content-Type': 'application/json' },
  });
});
it.each([
  [404, 'Postal code not found', 'missing'],
  [404, 'Bank not found', 'missing'],
  [404, 'Not Found', 'route'],
  [502, 'Unavailable', 'provider'],
  [504, 'Timeout', 'timeout'],
  [429, 'Too many requests', 'limited'],
  [400, 'Invalid', 'validation'],
  [500, 'Error', 'server'],
])('distinguishes HTTP %s (%s) as %s', async (status, title, kind) => {
  vi.stubGlobal(
    'fetch',
    vi
      .fn()
      .mockResolvedValue(
        new Response(JSON.stringify({ title }), { status, headers: { 'Retry-After': '12' } }),
      ),
  );
  await expect(
    execute({ method: 'GET', url: '/api/v1/br/addresses/07025130' }, new AbortController().signal),
  ).rejects.toMatchObject({ kind, status, retryAfter: 12 });
});
it('rejects HTML success pages and reports network errors', async () => {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue(new Response('<html></html>')));
  await expect(
    execute({ method: 'GET', url: '/api/v1/br/banks' }, new AbortController().signal),
  ).rejects.toMatchObject({ kind: 'unexpected' });
  vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new TypeError('Failed to fetch')));
  await expect(
    execute({ method: 'GET', url: '/api/v1/br/banks' }, new AbortController().signal),
  ).rejects.toMatchObject({ kind: 'network' });
});
