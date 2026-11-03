using System.Net.Http.Json;

var baseUrl = args.Length > 0 ? args[0].TrimEnd('/') : "http://localhost:5146";
using var client = new HttpClient();
using var response = await client.PostAsJsonAsync($"{baseUrl}/api/v1/finance/compound-interest",
    new { principal = 1000m, ratePerPeriod = 0.01m, periods = 12 });
response.EnsureSuccessStatusCode();
Console.WriteLine(await response.Content.ReadAsStringAsync());
