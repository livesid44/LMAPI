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
/// </summary>
public class LogicMonitorAuthHandler : DelegatingHandler
{
    private readonly string _accessId;
    private readonly string _accessKey;

    public LogicMonitorAuthHandler(string accessId, string accessKey)
    {
        _accessId = accessId;
        _accessKey = accessKey;
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

        return await base.SendAsync(request, cancellationToken);
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
