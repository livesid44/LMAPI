using System.Net.Http.Headers;
using System.Text;

namespace LMAPI.Infrastructure;

/// <summary>
/// DelegatingHandler that adds a <c>Authorization: Bearer {token}</c> header to every
/// outgoing LogicMonitor REST API v3 request.
/// <para>
/// The handler also logs an equivalent <c>curl</c> command at <c>Information</c> level
/// before each request, making it easy to reproduce the call in Postman or a terminal.
/// </para>
/// </summary>
public class LogicMonitorAuthHandler : DelegatingHandler
{
    private readonly string _bearerToken;
    private readonly ILogger<LogicMonitorAuthHandler> _logger;

    public LogicMonitorAuthHandler(
        string bearerToken,
        ILogger<LogicMonitorAuthHandler> logger)
    {
        _bearerToken = bearerToken;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", _bearerToken);

        // Read body (if any) — needed for the curl log
        var body = string.Empty;
        if (request.Content is not null)
            body = await request.Content.ReadAsStringAsync(cancellationToken);

        // Log an equivalent curl command at Information level so it is always visible
        // in every environment (including Azure Container Apps) without any extra config.
        _logger.LogInformation(
            "Outgoing LogicMonitor request — equivalent curl:\n{Curl}",
            BuildCurlCommand(request, body));

        return await base.SendAsync(request, cancellationToken);
    }

    /// <summary>
    /// Builds a <c>curl</c> command string that reproduces the given request exactly,
    /// including all headers and (optionally) a request body.
    /// </summary>
    public static string BuildCurlCommand(HttpRequestMessage request, string body)
    {
        var sb = new StringBuilder();
        sb.Append($"curl -X {request.Method.Method.ToUpperInvariant()} '{request.RequestUri?.AbsoluteUri}'");

        foreach (var (name, values) in request.Headers)
            foreach (var value in values)
                sb.Append($" \\\n  -H '{name}: {value}'");

        if (request.Content is not null)
            foreach (var (name, values) in request.Content.Headers)
                foreach (var value in values)
                    sb.Append($" \\\n  -H '{name}: {value}'");

        if (!string.IsNullOrEmpty(body))
            sb.Append($" \\\n  -d '{body}'");

        return sb.ToString();
    }
}
