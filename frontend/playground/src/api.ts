export interface PostalAddress {
  postalCode: string;
  street: string;
  complement: string;
  neighborhood: string;
  city: string;
  state: string;
  ibgeCode: string;
}
export interface ApiRequest {
  method: 'GET' | 'POST';
  url: string;
  body?: Record<string, unknown>;
}
export type Failure =
  | 'missing'
  | 'route'
  | 'timeout'
  | 'provider'
  | 'limited'
  | 'network'
  | 'unexpected'
  | 'validation'
  | 'server';
export class RequestError extends Error {
  constructor(
    public kind: Failure,
    public status?: number,
    public payload?: unknown,
    public retryAfter?: number,
  ) {
    super(kind);
  }
}
export async function execute(request: ApiRequest, signal: AbortSignal) {
  let response: Response;
  try {
    response = await fetch(request.url, {
      method: request.method,
      signal,
      headers: {
        Accept: 'application/json',
        ...(request.method === 'POST' ? { 'Content-Type': 'application/json' } : {}),
      },
      ...(request.method === 'POST' ? { body: JSON.stringify(request.body) } : {}),
    });
  } catch (error) {
    if (signal.aborted) throw error;
    throw new RequestError('network');
  }
  const text = await response.text();
  let data: unknown;
  try {
    data = JSON.parse(text);
  } catch {
    data = text;
  }
  if (!response.ok) {
    const title = data && typeof data === 'object' && 'title' in data ? data.title : null;
    const kind: Failure =
      response.status === 404
        ? title === 'Not Found' || !title
          ? 'route'
          : 'missing'
        : response.status === 504
          ? 'timeout'
          : response.status === 502
            ? 'provider'
            : response.status === 429
              ? 'limited'
              : response.status === 400
                ? 'validation'
                : response.status >= 500
                  ? 'server'
                  : 'unexpected';
    const retry = Number(response.headers.get('Retry-After'));
    throw new RequestError(
      kind,
      response.status,
      data,
      Number.isFinite(retry) && retry > 0 ? Math.ceil(retry) : undefined,
    );
  }
  if (data === null || typeof data !== 'object')
    throw new RequestError('unexpected', response.status, data);
  return { data, status: response.status };
}
