using LMAPI.Infrastructure;
using LMAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers & API exploration ───────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "LogicMonitor API Gateway",
        Version = "v1",
        Description = "A .NET 8 Web API that internally calls the LogicMonitor REST API v3 " +
                      "to expose device details, device events, device alerts, and LM Logs by device ID. " +
                      "Authentication uses the LMv1 HMAC-SHA256 token scheme."
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

// ── LogicMonitor configuration ───────────────────────────────────────────────
var lmSection = builder.Configuration.GetSection("LogicMonitor");
var company   = lmSection["Company"]   ?? throw new InvalidOperationException("LogicMonitor:Company is not configured.");
var accessId  = lmSection["AccessId"]  ?? throw new InvalidOperationException("LogicMonitor:AccessId is not configured.");
var accessKey = lmSection["AccessKey"] ?? throw new InvalidOperationException("LogicMonitor:AccessKey is not configured.");

var baseUrl = $"https://{company}.logicmonitor.com/santaba/rest/";

// ── Register the LMv1 auth handler and typed HttpClient ─────────────────────
builder.Services.AddTransient(_ => new LogicMonitorAuthHandler(accessId, accessKey));

builder.Services
    .AddHttpClient<ILogicMonitorService, LogicMonitorService>(client =>
    {
        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
        client.DefaultRequestHeaders.Add("X-Version", "3");
    })
    .AddHttpMessageHandler<LogicMonitorAuthHandler>();

// ── Build & middleware ───────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
