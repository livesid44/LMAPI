using LMAPI.Infrastructure;
using LMAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Application Insights telemetry ────────────────────────────────────────────
builder.Services.AddApplicationInsightsTelemetry();

// ── Health checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

// ── Controllers & API exploration ─────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "LogicMonitor API Gateway",
        Version = "v1",
        Description =
            "A .NET 8 Web API that internally calls the LogicMonitor REST API v3 " +
            "to expose device details, device events, device alerts, and LM Logs by device ID. " +
            "Authentication uses the LMv1 HMAC-SHA256 token scheme. " +
            "Also implements an MCP (Model Context Protocol) server at POST /mcp."
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

// ── LogicMonitor configuration ────────────────────────────────────────────────
// In Azure, these values come from Container App secrets backed by Key Vault.
// Locally, set them in appsettings.Development.json or user secrets.
var lmSection = builder.Configuration.GetSection("LogicMonitor");
var company   = lmSection["Company"]   ?? throw new InvalidOperationException("LogicMonitor:Company is not configured.");
var accessId  = lmSection["AccessId"]  ?? throw new InvalidOperationException("LogicMonitor:AccessId is not configured.");
var accessKey = lmSection["AccessKey"] ?? throw new InvalidOperationException("LogicMonitor:AccessKey is not configured.");

// Detect un-replaced placeholder values so the app fails fast with a clear message
// instead of making a live HTTP request to "your-company.logicmonitor.com" and getting
// a confusing DNS error.
static void AssertNotPlaceholder(string value, string settingPath, string placeholder)
{
    if (string.Equals(value, placeholder, StringComparison.OrdinalIgnoreCase))
        throw new InvalidOperationException(
            $"'{settingPath}' is still set to the placeholder value \"{placeholder}\". " +
            $"Set the real value via an environment variable (LogicMonitor__{settingPath.Split(':')[1]}), " +
            $".NET user secrets (dotnet user-secrets set \"{settingPath}\" \"<value>\"), " +
            $"or override it in appsettings.Development.json.");
}

AssertNotPlaceholder(company,   "LogicMonitor:Company",   "your-company");
AssertNotPlaceholder(accessId,  "LogicMonitor:AccessId",  "your-access-id");
AssertNotPlaceholder(accessKey, "LogicMonitor:AccessKey", "your-access-key");

var baseUrl = $"https://{company}.logicmonitor.com/santaba/rest/";

// ── Register the LMv1 auth handler and typed HttpClient ───────────────────────
builder.Services.AddTransient(_ => new LogicMonitorAuthHandler(accessId, accessKey));

builder.Services
    .AddHttpClient<ILogicMonitorService, LogicMonitorService>(client =>
    {
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("X-Version", "3");
    })
    .AddHttpMessageHandler<LogicMonitorAuthHandler>();

// ── Build & middleware ────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

// Liveness / readiness probe — used by Azure Container Apps and the Dockerfile HEALTHCHECK
app.MapHealthChecks("/health");

app.Run();
