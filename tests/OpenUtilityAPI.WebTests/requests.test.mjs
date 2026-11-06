import assert from "node:assert/strict";
import test from "node:test";
import { tools } from "../../src/OpenUtilityAPI.Api/wwwroot/tools.js";
import { buildRequest } from "../../src/OpenUtilityAPI.Api/wwwroot/requests.js";

test("All 22 tools build requests and the original 15 remain POST operations", () => {
  assert.equal(tools.length, 22);
  assert.equal(new Set(tools.map(tool => tool.id)).size, tools.length);
  assert.equal(tools.filter(tool => tool.method === "GET").length, 7);
  assert.equal(tools.filter(tool => !tool.method).length, 15);
  for (const tool of tools) {
    const values = Object.fromEntries(tool.fields.map(field => [field.key, field.value]));
    const request = buildRequest(tool, values);
    assert.ok(request.url.startsWith("/api/v1/"));
    assert.ok(!request.url.includes("{"));
    if (request.method === "GET") assert.equal("body" in request, false);
    else assert.deepEqual(request.body, values);
  }
});

test("Lookup URLs preserve leading zeros and case while encoding path segments", () => {
  const cep = tools.find(tool => tool.id === "cep");
  assert.equal(buildRequest(cep, { postalCode: "01001-000" }).url, "/api/v1/br/addresses/01001-000");
  assert.equal(buildRequest(tools.find(tool => tool.id === "bank"), { code: "001" }).url, "/api/v1/br/banks/001");
  assert.equal(buildRequest(tools.find(tool => tool.id === "municipalities"), { state: "sp" }).url,
    "/api/v1/br/localities/states/sp/municipalities");
  assert.equal(buildRequest(cep, { postalCode: "x/y?#" }).url, "/api/v1/br/addresses/x%2Fy%3F%23");
  assert.throws(() => buildRequest(cep, { postalCode: "" }));
});

test("Parameterless catalogs build GET requests without bodies", () => {
  for (const id of ["states", "banks", "indicator-catalog"]) {
    const request = buildRequest(tools.find(tool => tool.id === id), {});
    assert.equal(request.method, "GET");
    assert.equal("body" in request, false);
  }
});
