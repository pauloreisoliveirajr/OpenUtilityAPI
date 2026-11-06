import { useState } from 'react';
import {
  Alert,
  Button,
  Checkbox,
  FormControlLabel,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import ContentCopyRounded from '@mui/icons-material/ContentCopyRounded';
import LocationOnOutlined from '@mui/icons-material/LocationOnOutlined';
import type { Tool, Values } from './catalog';
import { unitGroups } from './catalog';
import { present, record } from './results';
import { messages, type Language } from './messages';
import { t as translate } from './shared/language.js';

export function Fields({
  tool,
  values,
  errors,
  language,
  onChange,
}: {
  tool: Tool;
  values: Values;
  errors: Record<string, string>;
  language: Language;
  onChange: (key: string, value: string | boolean) => void;
}) {
  const t = (key: string) => translate(key, language);
  return (
    <div className="fields">
      {tool.fields.map((field) => {
        const id = `field-${field.key}`;
        if (field.type === 'checkbox')
          return (
            <FormControlLabel
              key={field.key}
              className="check-field"
              label={t(field.label)}
              control={
                <Checkbox
                  id={id}
                  name={field.key}
                  checked={values[field.key] === true}
                  onChange={(e) => onChange(field.key, e.target.checked)}
                />
              }
            />
          );
        const select = field.type === 'select' || field.type === 'unit';
        return (
          <TextField
            fullWidth
            key={field.key}
            id={id}
            name={field.key}
            label={t(field.label)}
            value={values[field.key] ?? ''}
            onChange={(e) => onChange(field.key, e.target.value)}
            type={select || ['textarea', 'holidays'].includes(field.type) ? 'text' : field.type}
            select={select}
            multiline={['textarea', 'holidays'].includes(field.type)}
            minRows={['textarea', 'holidays'].includes(field.type) ? 3 : undefined}
            required={!field.allowEmpty && field.type !== 'holidays'}
            error={!!errors[field.key]}
            helperText={errors[field.key] || (field.hint ? t(field.hint) : undefined)}
            slotProps={{
              select: { native: true },
              inputLabel: field.type === 'date' ? { shrink: true } : undefined,
              htmlInput: {
                min: field.type === 'date' ? '0001-01-01' : field.min,
                max: field.type === 'date' ? '9999-12-31' : field.max,
                step: field.step,
                maxLength: field.type === 'holidays' ? 4026 : field.maxLength,
                pattern: field.pattern,
                autoComplete: 'off',
                spellCheck: false,
                inputMode: field.key === 'postalCode' ? 'numeric' : undefined,
              },
            }}
          >
            {select &&
              (field.type === 'unit'
                ? Object.entries(unitGroups).map(([group, options]) => (
                    <optgroup key={group} label={t(group)}>
                      {options.map(([value, label]) => (
                        <option key={value} value={value}>
                          {t(label)}
                        </option>
                      ))}
                    </optgroup>
                  ))
                : field.choices?.map(([value, label]) => (
                    <option key={value} value={value}>
                      {t(label)}
                    </option>
                  )))}
          </TextField>
        );
      })}
    </div>
  );
}

export function Success({
  tool,
  data,
  language,
  notify,
}: {
  tool: Tool;
  data: unknown;
  language: Language;
  notify: (message: 'copied' | 'copyFailed') => void;
}) {
  const m = messages[language];
  const t = (key: string) => translate(key, language);
  const [page, setPage] = useState(0);
  const [perPage, setPerPage] = useState(25);
  if (tool.id === 'cep') {
    const address = record(data);
    const s = (key: string) => (typeof address[key] === 'string' ? (address[key] as string) : '');
    if (!s('postalCode') || !s('city')) return <Alert severity="error">{m.unexpectedText}</Alert>;
    const cep = s('postalCode').replace(/^(\d{5})(\d{3})$/, '$1-$2');
    const rows = [
      [m.neighborhood, s('neighborhood')],
      [m.city, `${s('city')} / ${s('state')}`],
      [m.complement, s('complement')],
      [m.ibge, s('ibgeCode')],
    ];
    async function copy() {
      try {
        await navigator.clipboard.writeText(
          [s('street'), s('complement'), s('neighborhood'), `${s('city')} / ${s('state')}`, cep]
            .filter(Boolean)
            .join('\n'),
        );
        notify('copied');
      } catch {
        notify('copyFailed');
      }
    }
    return (
      <div className="address-result">
        <div className="address-top">
          <span className="address-icon">
            <LocationOnOutlined />
          </span>
          <div>
            <p className="eyebrow">{m.success}</p>
            <Typography component="h3" sx={{ fontSize: 25, fontWeight: 750 }}>
              {cep}
            </Typography>
          </div>
        </div>
        <Typography className="street">{s('street') || m.absent}</Typography>
        <dl className="address-details">
          {rows.map(([label, value]) => (
            <div key={label}>
              <dt>{label}</dt>
              <dd>{value || m.absent}</dd>
            </div>
          ))}
        </dl>
        <Button onClick={copy} size="small" startIcon={<ContentCopyRounded />} sx={{ mt: 1 }}>
          {m.copy}
        </Button>
      </div>
    );
  }
  const model = present(tool, data, language);
  const table = model.table;
  return (
    <div className="formatted-result">
      <p className="eyebrow">{model.heading}</p>
      <Typography
        component="h3"
        className={`result-value ${model.valid === false ? 'invalid-value' : ''}`}
      >
        {model.value}
      </Typography>
      {model.note && (
        <Typography variant="body2" color="text.secondary" sx={{ mt: 1, lineHeight: 1.8 }}>
          {model.note}
        </Typography>
      )}
      {model.rows && (
        <dl className="result-rows">
          {model.rows.map(([label, value], index) => (
            <div key={index}>
              <dt>{label}</dt>
              <dd>{value}</dd>
            </div>
          ))}
        </dl>
      )}
      {table && (
        <>
          <TableContainer
            className="data-table"
            tabIndex={0}
            aria-label={t(
              tool.id === 'price' || tool.id === 'sac' ? 'Tabela de parcelas' : 'Dados da consulta',
            )}
          >
            <Table size="small" stickyHeader>
              <TableHead>
                <TableRow>
                  {table.headers.map((label, i) => (
                    <TableCell key={i} scope="col">
                      {label}
                    </TableCell>
                  ))}
                </TableRow>
              </TableHead>
              <TableBody>
                {table.rows.slice(page * perPage, (page + 1) * perPage).map((row, index) => (
                  <TableRow key={index}>
                    {row.map((value, i) => (
                      <TableCell key={i}>{value}</TableCell>
                    ))}
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
          {table.rows.length > 25 && (
            <TablePagination
              component="div"
              count={table.rows.length}
              page={page}
              rowsPerPage={perPage}
              rowsPerPageOptions={[25, 50, 100]}
              onPageChange={(_, value) => setPage(value)}
              onRowsPerPageChange={(e) => {
                setPerPage(Number(e.target.value));
                setPage(0);
              }}
              labelRowsPerPage={language === 'pt-BR' ? 'Linhas por página' : 'Rows per page'}
              labelDisplayedRows={({ from, to, count }) =>
                `${from}–${to} ${language === 'pt-BR' ? 'de' : 'of'} ${count}`
              }
              getItemAriaLabel={(type) =>
                language === 'pt-BR'
                  ? `Ir para a ${type === 'next' ? 'próxima' : type === 'previous' ? 'anterior' : type === 'first' ? 'primeira' : 'última'} página`
                  : `Go to ${type} page`
              }
            />
          )}
        </>
      )}
    </div>
  );
}
