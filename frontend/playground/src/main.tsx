import { StrictMode, useEffect, useMemo, useRef, useState, type FormEvent } from 'react';
import { createRoot } from 'react-dom/client';
import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  CssBaseline,
  Drawer,
  IconButton,
  InputAdornment,
  MenuItem,
  Paper,
  Snackbar,
  TextField,
  ThemeProvider,
  Tooltip,
  Typography,
} from '@mui/material';
import ArrowForwardRounded from '@mui/icons-material/ArrowForwardRounded';
import CheckCircleRounded from '@mui/icons-material/CheckCircleRounded';
import CodeRounded from '@mui/icons-material/CodeRounded';
import ExpandMoreRounded from '@mui/icons-material/ExpandMoreRounded';
import GitHub from '@mui/icons-material/GitHub';
import LanguageRounded from '@mui/icons-material/LanguageRounded';
import LocationOnOutlined from '@mui/icons-material/LocationOnOutlined';
import LockOutlined from '@mui/icons-material/LockOutlined';
import PublicRounded from '@mui/icons-material/PublicRounded';
import SearchRounded from '@mui/icons-material/SearchRounded';
import MenuRounded from '@mui/icons-material/MenuRounded';
import CloseRounded from '@mui/icons-material/CloseRounded';
import CalculateOutlined from '@mui/icons-material/CalculateOutlined';
import BadgeOutlined from '@mui/icons-material/BadgeOutlined';
import SwapHorizRounded from '@mui/icons-material/SwapHorizRounded';
import EventOutlined from '@mui/icons-material/EventOutlined';
import TextFieldsRounded from '@mui/icons-material/TextFieldsRounded';
import '@fontsource-variable/plus-jakarta-sans';
import { execute, RequestError, type ApiRequest } from './api';
import { FieldError, initialValues, payload, tools, type Values } from './catalog';
import { Fields, Success } from './components';
import { messages, type Language } from './messages';
import { record } from './results';
import { buildRequest } from './shared/requests.js';
import { t as translate } from './shared/language.js';
import { theme } from './theme';
import './style.css';

function readLanguage(): Language {
  try {
    return localStorage.getItem('openutilityapi.language') === 'en-US' ? 'en-US' : 'pt-BR';
  } catch {
    return 'pt-BR';
  }
}
function hashTool() {
  return (
    tools.find((tool) => location.hash === `#${tool.id}`) ??
    tools.find((tool) => tool.id === 'cep')!
  );
}
const categoryIcons: Record<string, typeof PublicRounded> = {
  'Consultas públicas': PublicRounded,
  'Documentos brasileiros': BadgeOutlined,
  Finanças: CalculateOutlined,
  Matemática: CalculateOutlined,
  Conversões: SwapHorizRounded,
  Datas: EventOutlined,
  Texto: TextFieldsRounded,
};
type Outcome = {
  request: ApiRequest;
  data?: unknown;
  status?: number;
  error?: RequestError;
  duration: number;
};

function App() {
  const [language, setLanguage] = useState<Language>(readLanguage);
  const m = messages[language];
  const t = (key: string) => translate(key, language);
  const [tool, setTool] = useState(hashTool);
  const [values, setValues] = useState<Values>(() => initialValues(hashTool()));
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [search, setSearch] = useState('');
  const [drawer, setDrawer] = useState(false);
  const [busy, setBusy] = useState(false);
  const [outcome, setOutcome] = useState<Outcome>();
  const [toast, setToast] = useState<'copied' | 'copyFailed'>();
  const current = useRef<AbortController | null>(null);
  const form = useRef<HTMLFormElement>(null);
  const publicLookup = tool.category === 'Consultas públicas';
  const ToolIcon = tool.id === 'cep' ? LocationOnOutlined : categoryIcons[tool.category];

  function clear() {
    current.current?.abort();
    current.current = null;
    setBusy(false);
    setOutcome(undefined);
    setErrors({});
  }
  useEffect(() => {
    function change() {
      const next = hashTool();
      clear();
      setTool(next);
      setValues(initialValues(next));
      setDrawer(false);
      document.getElementById('workspace')?.focus({ preventScroll: true });
    }
    window.addEventListener('hashchange', change);
    return () => {
      window.removeEventListener('hashchange', change);
      current.current?.abort();
    };
  }, []);
  useEffect(() => {
    document.documentElement.lang = language;
    document.title = `OpenUtilityAPI · ${translate(tool.title, language)}`;
    document
      .querySelector('meta[name="description"]')
      ?.setAttribute(
        'content',
        translate(
          'Experimente as utilidades da OpenUtilityAPI. Valide documentos, calcule juros e converta valores diretamente no navegador.',
          language,
        ),
      );
    try {
      localStorage.setItem('openutilityapi.language', language);
    } catch {
      /* Preference is optional. */
    }
  }, [language, tool]);

  const request = useMemo(() => {
    try {
      return buildRequest(tool, payload(tool, values)) as ApiRequest;
    } catch {
      return null;
    }
  }, [tool, values]);

  async function submit(event: FormEvent) {
    event.preventDefault();
    if (busy) return;
    const invalid = form.current?.querySelector<
      HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement
    >(':invalid');
    if (invalid) {
      setErrors({ [invalid.name]: 'invalidField' });
      invalid.focus();
      return;
    }
    let built: ApiRequest;
    try {
      built = buildRequest(tool, payload(tool, values)) as ApiRequest;
    } catch (error) {
      if (error instanceof FieldError) {
        setErrors({ [error.field]: error.messageKey });
        form.current?.querySelector<HTMLElement>(`[name="${error.field}"]`)?.focus();
      }
      return;
    }
    clear();
    const controller = new AbortController();
    current.current = controller;
    setBusy(true);
    const started = performance.now();
    const timeout = setTimeout(() => controller.abort('timeout'), 20000);
    try {
      const response = await execute(built, controller.signal);
      if (current.current === controller)
        setOutcome({
          request: built,
          ...response,
          duration: Math.round(performance.now() - started),
        });
    } catch (error) {
      if (current.current !== controller) return;
      const failure =
        controller.signal.reason === 'timeout'
          ? new RequestError('timeout')
          : error instanceof RequestError
            ? error
            : new RequestError('unexpected');
      setOutcome({
        request: built,
        data: failure.payload,
        status: failure.status,
        error: failure,
        duration: Math.round(performance.now() - started),
      });
    } finally {
      clearTimeout(timeout);
      if (current.current === controller) {
        setBusy(false);
        current.current = null;
      }
    }
  }

  const searchable = (value: string) =>
    value
      .toLocaleLowerCase(language)
      .normalize('NFD')
      .replace(/\p{Diacritic}/gu, '');
  const filtered = tools.filter((item) =>
    searchable(`${t(item.title)} ${t(item.category)} ${item.id}`).includes(
      searchable(search.trim()),
    ),
  );
  const categories = ['Consultas públicas', ...new Set(tools.filter((item) => item.category !== 'Consultas públicas').map((item) => item.category))]
    .filter((category) => filtered.some((item) => item.category === category));
  const errorDetails = Object.keys(record(record(outcome?.error?.payload).errors)).map((key) =>
    t(tool.fields.find((f) => f.key.toLowerCase() === key.toLowerCase())?.label ?? 'Requisição'),
  );
  const displayRequest = outcome?.request ?? request;
  function catalog(mobile: boolean) {
    return (
      <aside className={`sidebar ${mobile ? 'mobile-sidebar' : ''}`} aria-label={m.catalog}>
        <div className="sidebar-title">
          <Typography component="h2" sx={{ fontSize: 13, fontWeight: 750 }}>
            {m.tools}
          </Typography>
          {mobile ? (
            <IconButton aria-label={m.closeMenu} onClick={() => setDrawer(false)}>
              <CloseRounded />
            </IconButton>
          ) : (
            <Chip size="small" label={tools.length} />
          )}
        </div>
        <TextField
          fullWidth
          size="small"
          placeholder={m.searchAll}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          slotProps={{
            htmlInput: { 'aria-label': m.searchAll },
            input: {
              startAdornment: (
                <InputAdornment position="start">
                  <SearchRounded fontSize="small" />
                </InputAdornment>
              ),
            },
          }}
        />
        <nav className="catalog-nav" aria-label={m.catalog}>
          {categories.map((category) => (
            <div key={category} className="nav-group">
              <p className="nav-label">{t(category)}</p>
              {filtered
                .filter((item) => item.category === category)
                .map((item) => {
                  const Icon =
                    item.id === 'cep' ? LocationOnOutlined : categoryIcons[item.category];
                  return (
                    <a
                      key={item.id}
                      href={`#${item.id}`}
                      onClick={() => setDrawer(false)}
                      className={`tool-link ${item.id === tool.id ? 'selected-tool' : ''}`}
                      aria-current={item.id === tool.id ? 'page' : undefined}
                    >
                      <Icon fontSize="small" />
                      <span>{t(item.title)}</span>
                      {item.id === tool.id && <span className="nav-dot" />}
                    </a>
                  );
                })}
            </div>
          ))}
          {filtered.length === 0 && (
            <Typography variant="body2" color="text.secondary">
              {m.noMatch}
            </Typography>
          )}
        </nav>
        <div className="sidebar-foot">
          <span className="open-dot" />
          {m.openSource}
        </div>
      </aside>
    );
  }

  return (
    <>
      <a
        className="skip"
        href="#workspace"
        onClick={(event) => {
          event.preventDefault();
          document.getElementById('workspace')?.focus();
        }}
      >
        {m.skip}
      </a>
      <header className="header">
        <IconButton className="mobile-menu" aria-label={m.menu} onClick={() => setDrawer(true)}>
          <MenuRounded />
        </IconButton>
        <a className="brand" href="/" aria-label="OpenUtilityAPI">
          <span className="brand-icon">
            <CodeRounded />
          </span>
          <span>
            OpenUtility<span className="brand-api">API</span>
          </span>
        </a>
        <span className="header-note">{m.allTools}</span>
        <div className="header-actions">
          <TextField
            select
            size="small"
            value={language}
            onChange={(e) => {
              setLanguage(e.target.value as Language);
              setErrors({});
            }}
            slotProps={{
              select: { inputProps: { 'aria-label': m.language } },
              input: {
                startAdornment: (
                  <LanguageRounded className="language-symbol" fontSize="small" sx={{ mr: 1, color: 'text.secondary' }} />
                ),
              },
            }}
          >
            <MenuItem value="pt-BR">Português</MenuItem>
            <MenuItem value="en-US">English</MenuItem>
          </TextField>
          <Tooltip title="GitHub">
            <IconButton
              component="a"
              href="https://github.com/pauloreisoliveirajr/OpenUtilityAPI"
              aria-label="GitHub"
            >
              <GitHub />
            </IconButton>
          </Tooltip>
        </div>
      </header>
      <Drawer
        open={drawer}
        onClose={() => setDrawer(false)}
        slotProps={{ paper: { sx: { width: 290 } } }}
      >
        {catalog(true)}
      </Drawer>
      <div className="shell">
        <div className="desktop-catalog">{catalog(false)}</div>
        <main className="main" id="workspace" tabIndex={-1}>
          <div className="breadcrumb">
            <span>OpenUtilityAPI</span>
            <span>/</span>
            <span>{t(tool.category)}</span>
          </div>
          <section className="hero">
            <div className="hero-copy">
              <p className="eyebrow">{publicLookup ? m.eyebrow : m.catalogNote}</p>
              <Typography component="h1" variant="h1">
                {tool.id === 'cep' ? m.title : t(tool.title)}
              </Typography>
              <Typography className="hero-description" color="text.secondary">
                {tool.id === 'cep' ? m.subtitle : t(tool.description)}
              </Typography>
              <div className="hero-tags">
                <span>
                  {publicLookup ? <PublicRounded /> : <CheckCircleRounded />}
                  {publicLookup ? m.external : m.local}
                </span>
                <span>
                  <CheckCircleRounded />
                  {m.noAccount}
                </span>
              </div>
            </div>
            <div className="hero-art" aria-hidden="true">
              <span className="art-orbit orbit-one" />
              <span className="art-orbit orbit-two" />
              <div className="map-card">
                <div className="map-lines" />
                <ToolIcon className="map-pin" />
                <span className="map-label">UTILITY</span>
              </div>
            </div>
          </section>
          <div className="lookup-grid">
            <Paper
              component="section"
              className="form-card"
              variant="outlined"
              aria-labelledby="form-title"
            >
              <div className="section-step">
                {t(tool.title)}
                <Chip size="small" label={tool.method ?? 'POST'} />
              </div>
              <Typography component="h2" variant="h2" id="form-title">
                {tool.id === 'cep' ? m.inputTitle : m.input}
              </Typography>
              <Typography className="section-description" color="text.secondary">
                {m.inputText}
              </Typography>
              <Box component="form" ref={form} onSubmit={submit} noValidate>
                <Fields
                  tool={tool}
                  values={values}
                  errors={Object.fromEntries(
                    Object.entries(errors).map(([key, message]) => [
                      key,
                      message === 'invalidField' ? m.invalidField : t(message),
                    ]),
                  )}
                  language={language}
                  onChange={(key, value) => {
                    clear();
                    setValues((previous) => ({ ...previous, [key]: value }));
                  }}
                />
                {tool.fields.length === 0 && <Alert severity="info">{m.noParameters}</Alert>}
                <Button
                  className="lookup-button"
                  fullWidth
                  variant="contained"
                  size="large"
                  type="submit"
                  disabled={busy}
                  endIcon={
                    busy ? <CircularProgress size={18} color="inherit" /> : <ArrowForwardRounded />
                  }
                >
                  {busy ? m.loading : m.execute}
                </Button>
                {tool.fields.length > 0 && (
                  <Button
                    className="example-button"
                    size="small"
                    onClick={() => {
                      clear();
                      setValues(initialValues(tool));
                    }}
                  >
                    {m.reset}
                  </Button>
                )}
              </Box>
              <div className="privacy-note">
                <LockOutlined />
                <span>
                  {tool.id === 'cep'
                    ? m.privacy
                    : t('Use dados de exemplo ao testar documentos pessoais.')}
                </span>
              </div>
            </Paper>
            <Paper
              component="section"
              className={`result-card ${outcome && !outcome.error ? 'has-address' : ''}`}
              variant="outlined"
              aria-labelledby="result-title"
              aria-busy={busy}
            >
              <div className="result-heading">
                <Typography id="result-title" component="h2" sx={{ fontWeight: 700, fontSize: 13 }}>
                  {m.result}
                </Typography>
                <Chip
                  size="small"
                  color={outcome?.status && !outcome.error ? 'success' : 'default'}
                  variant="outlined"
                  label={busy ? m.loading : outcome?.status ? `HTTP ${outcome.status}` : outcome?.error ? m.noResponse : m.ready}
                />
              </div>
              <div aria-live="polite">
                {busy ? (
                  <div className="empty-result">
                    <CircularProgress size={34} />
                    <Typography color="text.secondary">{m.loading}</Typography>
                  </div>
                ) : outcome?.error ? (
                  <Alert
                    severity={outcome.error.kind === 'missing' ? 'info' : 'error'}
                    sx={{ mt: 3 }}
                  >
                    <Typography sx={{ fontWeight: 700, fontSize: 14 }}>
                      {m[outcome.error.kind]}
                    </Typography>
                    <Typography sx={{ fontSize: 13, mt: 1 }}>
                      {outcome.error.kind === 'timeout' && !outcome.status
                        ? m.requestTimeout
                        : m[`${outcome.error.kind}Text`]}
                    </Typography>
                    {outcome.error.retryAfter && outcome.error.kind === 'limited' && (
                      <p>
                        {m.retry} {outcome.error.retryAfter} {m.seconds}
                      </p>
                    )}
                    {errorDetails.length > 0 && (
                      <ul>
                        {errorDetails.map((label, i) => (
                          <li key={i}>
                            {label}: {m.invalidField}
                          </li>
                        ))}
                      </ul>
                    )}
                  </Alert>
                ) : outcome ? (
                  <Success
                    key={`${tool.id}-${outcome.duration}`}
                    tool={tool}
                    data={outcome.data}
                    language={language}
                    notify={setToast}
                  />
                ) : (
                  <div className="empty-result">
                    <div className="empty-icon">
                      <ToolIcon />
                    </div>
                    <Typography component="h3" sx={{ fontWeight: 700 }}>
                      {m.emptyTitle}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {m.emptyText}
                    </Typography>
                  </div>
                )}
              </div>
            </Paper>
          </div>
          <div className="source-note">
            <PublicRounded fontSize="small" />
            <span>{t(tool.limit)}</span>
          </div>
          <Accordion className="technical" disableGutters elevation={0}>
            <AccordionSummary expandIcon={<ExpandMoreRounded />}>
              <div className="technical-title">
                <span className="code-icon">
                  <CodeRounded />
                </span>
                <div>
                  <Typography sx={{ fontWeight: 700, fontSize: 14 }}>{m.technical}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    {m.technicalText}
                  </Typography>
                </div>
              </div>
              <Chip className="get-chip" size="small" label={tool.method ?? 'POST'} />
            </AccordionSummary>
            <AccordionDetails>
              <div className="endpoint">
                <Chip size="small" color="success" label={tool.method ?? 'POST'} />
                <code>{displayRequest?.url ?? `/api/v1/${tool.path}`}</code>
                {outcome && <span>{outcome.duration} ms</span>}
              </div>
              <div className="json-grid">
                <div>
                  <h3>{m.request}</h3>
                  <pre>
                    {displayRequest
                      ? JSON.stringify(displayRequest, null, 2)
                      : t('Complete os campos para gerar a requisição.')}
                  </pre>
                </div>
                <div>
                  <h3>{m.response}</h3>
                  <pre>
                    {outcome ? JSON.stringify(outcome.data ?? null, null, 2) : m.pendingJson}
                  </pre>
                </div>
              </div>
            </AccordionDetails>
          </Accordion>
          <footer>
            <span>{m.footer}</span>
            <span>{m.footerNote}</span>
          </footer>
        </main>
      </div>
      <Snackbar
        open={!!toast}
        onClose={() => setToast(undefined)}
        autoHideDuration={4000}
        message={toast ? m[toast] : ''}
      />
    </>
  );
}
createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <App />
    </ThemeProvider>
  </StrictMode>,
);
