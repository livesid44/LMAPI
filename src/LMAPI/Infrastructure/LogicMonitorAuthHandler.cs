using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

namespace LMAPI.Infrastructure;

/// <summary>
/// DelegatingHandler that adds the LogicMonitor LMv1 HMAC-SHA256 authentication header
/// to every outgoing request.
/// <para>
/// Signature formula (from LogicMonitor docs):
/// <c>Base64( HMAC-SHA256( AccessKey, HTTPMethod + EpochMs + RequestBody + ResourcePath ) )</c>
/// </para>
/// <para>
/// When the <c>Debug</c> log level is enabled for <c>LMAPI.Infrastructure</c> the handler
/// logs an equivalent <c>curl</c> command before each request, which makes it easy to
/// reproduce a request manually and verify that the credentials and signature are correct.
/// </para>
/// </summary>
public class LogicMonitorAuthHandler : DelegatingHandler
{
    private readonly string _accessId;
    private readonly string _accessKey;
    private readonly ILogger<LogicMonitorAuthHandler> _logger;

    public LogicMonitorAuthHandler(
        string accessId,
        string accessKey,
        ILogger<LogicMonitorAuthHandler> logger)
    {
        _accessId = accessId;
        _accessKey = accessKey;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var epochMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        var method = request.Method.Method.ToUpperInvariant();

        // Read body (if any) — needed for POST/PUT signature
        var body = string.Empty;
        if (request.Content is not null)
            body = await request.Content.ReadAsStringAsync(cancellationToken);

        // Resource path is the absolute path only (NO query string).
        // The LMv1 spec signs: Method + EpochMs + Body + Path — query parameters
        // must be excluded or the HMAC will not match what LogicMonitor computes,
        // causing a 401 Unauthorized.
        var resourcePath = request.RequestUri?.AbsolutePath ?? string.Empty;

        var stringToSign = $"{method}{epochMs}{body}{resourcePath}";
        var signature = ComputeSignature(stringToSign, _accessKey);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("LMv1", $"{_accessId}:{signature}:{epochMs}");

        // Log an equivalent curl command so the request can be reproduced manually.
        // Guarded by IsEnabled so the string allocation is skipped in production
        // unless Debug logging is explicitly turned on.
        if (_logger.IsEnabled(LogLevel.Debug))
            _logger.LogDebug(
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

    public static string ComputeSignature(string stringToSign, string accessKey)
    {
        var keyBytes = Encoding.UTF8.GetBytes(accessKey);
        var messageBytes = Encoding.UTF8.GetBytes(stringToSign);

        using var hmac = new HMACSHA256(keyBytes);
        var hashBytes = hmac.ComputeHash(messageBytes);
        return Convert.ToBase64String(hashBytes);
    }
}
