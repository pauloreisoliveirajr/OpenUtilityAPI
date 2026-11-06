import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
import { english, t, setLanguage, getLanguage, restoreLanguage } from "../../src/OpenUtilityAPI.Api/wwwroot/language.js";
import { tools } from "../../src/OpenUtilityAPI.Api/wwwroot/tools.js";

test("English covers every tool, field, hint and choice without changing API values", () => {
  const unchanged = new Set(["CPF", "CNPJ"]);
  for (const tool of tools) {
    const texts = [tool.category, tool.title, tool.description, tool.limit];
    for (const field of tool.fields) {
      texts.push(field.label);
      if (field.hint) texts.push(field.hint);
      for (const [, label] of field.choices ?? []) texts.push(label);
    }
    for (const text of texts) assert.ok(english[text] || unchanged.has(text), "Missing translation: " + text);
  }
});

test("Static content and interface messages have English translations", async () => {
  const source = await readFile(new URL("../../src/OpenUtilityAPI.Api/wwwroot/playground.js", import.meta.url), "utf8");
  const html = await readFile(new URL("../../src/OpenUtilityAPI.Api/wwwroot/index.html", import.meta.url), "utf8");
  const keys = [
    ...Array.from(source.matchAll(/\bt\("([^"]+)"\)/g), match => match[1]),
    ...Array.from(html.matchAll(/data-i18n(?:-label)?="([^"]+)"/g), match => match[1])
  ];
  assert.ok(keys.length > 50);
  for (const key of keys) assert.ok(english[key], "Missing translation: " + key);
});

test("Language preference survives restoring and rejects unsupported values", () => {
  const values = new Map();
  Object.defineProperty(globalThis, "localStorage", {
    configurable: true,
    value: { getItem: key => values.get(key), setItem: (key, value) => values.set(key, value) }
  });
  setLanguage("en-US");
  restoreLanguage();
  assert.equal(getLanguage(), "en-US");
  assert.equal(t("Juros compostos"), "Compound interest");
  setLanguage("pt-BR");
  assert.equal(t("Juros compostos"), "Juros compostos");
  values.set("openutilityapi.language", "unsupported");
  restoreLanguage();
  assert.equal(getLanguage(), "pt-BR");
  delete globalThis.localStorage;
});

test("Unavailable browser storage does not prevent switching languages", () => {
  Object.defineProperty(globalThis, "localStorage", {
    configurable: true,
    get() { throw new Error("Storage blocked"); }
  });
  assert.doesNotThrow(() => setLanguage("en-US"));
  assert.equal(t("Resultado"), "Result");
  assert.doesNotThrow(restoreLanguage);
  assert.equal(getLanguage(), "pt-BR");
  delete globalThis.localStorage;
});
