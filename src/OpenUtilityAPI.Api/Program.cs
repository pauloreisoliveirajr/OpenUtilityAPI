using System.Threading.RateLimiting;
using OpenUtilityAPI.Api;
using OpenUtilityAPI.Brazil;
using OpenUtilityAPI.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 65536);
builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddSingleton<IndicatorCache>();
builder.Services.AddHttpClient<IIndicatorLookup, BcbIndicatorLookup>(client =>
{
    client.BaseAddress = new Uri("https://api.bcb.gov.br/");
    client.Timeout = TimeSpan.FromSeconds(5);
    client.MaxResponseContentBufferSize = 32768;
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddSingleton<BankDirectoryCache>();
builder.Services.AddHttpClient<IBankLookup, BrasilApiBankLookup>(client =>
{
    client.BaseAddress = new Uri("https://brasilapi.com.br/");
    client.Timeout = TimeSpan.FromSeconds(5);
    client.MaxResponseContentBufferSize = 2097152;
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddSingleton<LocalityCache>();
builder.Services.AddHttpClient<ILocalityLookup, IbgeLocalityLookup>(client =>
{
    client.BaseAddress = new Uri("https://servicodados.ibge.gov.br/");
    client.Timeout = TimeSpan.FromSeconds(5);
    client.MaxResponseContentBufferSize = 2097152;
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddSingleton<PostalAddressCache>();
builder.Services.AddHttpClient<IPostalAddressLookup, ViaCepAddressLookup>(client =>
{
    client.BaseAddress = new Uri("https://viacep.com.br/");
    client.Timeout = TimeSpan.FromSeconds(5);
    client.MaxResponseContentBufferSize = 32768;
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "OpenUtilityAPI.Api.xml"));
    options.SchemaFilter<RecordRequestSchemaFilter>();
});
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [])
        .WithMethods("GET", "POST").WithHeaders("Content-Type")));
builder.Services.AddRateLimiter(options =>
{
    var limit = builder.Configuration.GetValue("RateLimit:RequestsPerMinute", 120);
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = limit,
                Window = TimeSpan.FromMinutes(1)
            }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
            context.HttpContext.Response.Headers.RetryAfter =
                Math.Ceiling(retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
        await Results.Problem(statusCode: 429, title: "Too many requests")
            .ExecuteAsync(context.HttpContext);
    };
});

var app = builder.Build();
app.UseExceptionHandler(new ExceptionHandlerOptions
{
    StatusCodeSelector = exception => exception is BadHttpRequestException badRequest
        ? badRequest.StatusCode
        : StatusCodes.Status500InternalServerError
});
app.UseStatusCodePages();
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Homologation"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors();
app.UseRateLimiter();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapHealthChecks("/health");
app.MapControllers();
app.Run();

/// <summary>The API entry point.</summary>
public partial class Program;

