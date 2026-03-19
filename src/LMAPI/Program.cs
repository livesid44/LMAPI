using LMAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Log Monitoring API Gateway",
        Version = "v1",
        Description = "A .NET Core API that internally calls the Log Monitoring API."
    });
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

// Register the typed HttpClient for the Log Monitoring API.
var logMonitoringBaseUrl = builder.Configuration["LogMonitoringApi:BaseUrl"]
    ?? throw new InvalidOperationException("LogMonitoringApi:BaseUrl is not configured.");

builder.Services.AddHttpClient<ILogMonitoringService, LogMonitoringService>(client =>
{
    client.BaseAddress = new Uri(logMonitoringBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
