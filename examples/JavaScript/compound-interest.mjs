const baseUrl = (process.argv[2] ?? "http://localhost:5146").replace(/\/+$/, "");
const response = await fetch(`${baseUrl}/api/v1/finance/compound-interest`, {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({ principal: 1000, ratePerPeriod: 0.01, periods: 12 }),
});
if (!response.ok) {
  throw new Error(`HTTP ${response.status}: ${await response.text()}`);
}
console.log(await response.text());
