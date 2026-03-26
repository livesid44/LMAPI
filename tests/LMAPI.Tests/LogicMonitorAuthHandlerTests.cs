using System.Text;
using LMAPI.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace LMAPI.Tests;

public class LogicMonitorAuthHandlerTests
{
    // ── ComputeSignature ─────────────────────────────────────────────────────

    [Fact]
    public void ComputeSignature_ReturnsDeterministicBase64()
    {
        // Given the same inputs, the HMAC-SHA256 signature must always be identical.
        const string key = "mysecretkey";
        const string msg = "GET1234567890/device/devices/42";

        var sig1 = LogicMonitorAuthHandler.ComputeSignature(msg, key);
        var sig2 = LogicMonitorAuthHandler.ComputeSignature(msg, key);

        Assert.Equal(sig1, sig2);
        // Must be non-empty Base64
        Assert.False(string.IsNullOrWhiteSpace(sig1));
        Convert.FromBase64String(sig1); // throws if not valid Base64
    }

    [Fact]
    public void ComputeSignature_DifferentKeys_ProduceDifferentSignatures()
    {
        const string msg = "GET9876543210/device/devices/1";
        var sig1 = LogicMonitorAuthHandler.ComputeSignature(msg, "key-alpha");
        var sig2 = LogicMonitorAuthHandler.ComputeSignature(msg, "key-beta");
        Assert.NotEqual(sig1, sig2);
    }

    [Fact]
    public void ComputeSignature_DifferentMessages_ProduceDifferentSignatures()
    {
        const string key = "sharedkey";
        var sig1 = LogicMonitorAuthHandler.ComputeSignature("GETabcdef/device/devices/1", key);
        var sig2 = LogicMonitorAuthHandler.ComputeSignature("GETabcdef/device/devices/2", key);
        Assert.NotEqual(sig1, sig2);
    }

    // ── Query string excluded from signature ─────────────────────────────────
    // The LMv1 spec signs:  Method + EpochMs + Body + ResourcePath
    // ResourcePath = absolute path only — query parameters must NOT be signed.
    // If query parameters were included, two requests to the same path with
    // different query strings would produce different signatures even though the
    // path is identical, causing 401 errors from LogicMonitor.

    [Fact]
    public async Task SendAsync_SignsAbsolutePathOnly_QueryStringExcluded()
    {
        HttpRequestMessage? captured = null;

        var inner = new DelegateHandler(req =>
        {
            captured = req;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        });

        var handler = new LogicMonitorAuthHandler("id", "key", NullLogger<LogicMonitorAuthHandler>.Instance) { InnerHandler = inner };
        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://test.logicmonitor.com/santaba/rest/")
        };

        // Two requests to the same path, different query strings.
        // Their signatures must be identical (query is not signed).
        await client.GetAsync("device/devices?size=50&offset=0");
        var parts1 = captured!.Headers.Authorization!.Parameter!.Split(':');
        var epochMs1 = parts1[2];
        var sig1 = parts1[1];

        captured = null;

        // Manually build the same path with different query so we can compare
        // just the path portion of the signature string.  We can verify the
        // property indirectly: build the expected signature ourselves using the
        // *path* and confirm it matches what the handler produced.
        var expectedSignature = LogicMonitorAuthHandler.ComputeSignature(
            $"GET{epochMs1}/santaba/rest/device/devices", "key");

        Assert.Equal(expectedSignature, sig1);
    }

    // ── Authorization header ─────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_AddsLMv1AuthorizationHeader()
    {
        HttpRequestMessage? captured = null;

        var inner = new DelegateHandler(req =>
        {
            captured = req;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        });

        var handler = new LogicMonitorAuthHandler("test-id", "test-key", NullLogger<LogicMonitorAuthHandler>.Instance)
        {
            InnerHandler = inner
        };

        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://test.logicmonitor.com/santaba/rest/")
        };

        await client.GetAsync("device/devices/42");

        Assert.NotNull(captured);
        Assert.NotNull(captured!.Headers.Authorization);
        Assert.Equal("LMv1", captured.Headers.Authorization.Scheme);

        // Format: <accessId>:<signature>:<epochMs>
        var parts = captured.Headers.Authorization.Parameter!.Split(':');
        Assert.Equal(3, parts.Length);
        Assert.Equal("test-id", parts[0]);
        Assert.False(string.IsNullOrWhiteSpace(parts[1]));
        Assert.True(long.TryParse(parts[2], out _));
    }

    // ── BuildCurlCommand ─────────────────────────────────────────────────────

    [Fact]
    public void BuildCurlCommand_IncludesMethodAndUrl()
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            new Uri("https://example.logicmonitor.com/santaba/rest/device/devices?size=50&offset=0"));
        request.Headers.Add("Authorization", "LMv1 id:sig:epoch");

        var curl = LogicMonitorAuthHandler.BuildCurlCommand(request, string.Empty);

        Assert.Contains("curl -X GET", curl);
        Assert.Contains("https://example.logicmonitor.com/santaba/rest/device/devices?size=50&offset=0", curl);
    }

    [Fact]
    public void BuildCurlCommand_IncludesAuthorizationHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            new Uri("https://example.logicmonitor.com/santaba/rest/device/devices"));
        request.Headers.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("LMv1", "myId:mySignature:12345");

        var curl = LogicMonitorAuthHandler.BuildCurlCommand(request, string.Empty);

        Assert.Contains("-H 'Authorization: LMv1 myId:mySignature:12345'", curl);
    }

    [Fact]
    public void BuildCurlCommand_IncludesBodyForPostRequests()
    {
        const string body = "{\"name\":\"test\"}";
        var request = new HttpRequestMessage(HttpMethod.Post,
            new Uri("https://example.logicmonitor.com/santaba/rest/device/devices"));

        var curl = LogicMonitorAuthHandler.BuildCurlCommand(request, body);

        Assert.Contains($"-d '{body}'", curl);
    }

    [Fact]
    public void BuildCurlCommand_OmitsBodyWhenEmpty()
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            new Uri("https://example.logicmonitor.com/santaba/rest/device/devices"));

        var curl = LogicMonitorAuthHandler.BuildCurlCommand(request, string.Empty);

        Assert.DoesNotContain("-d ", curl);
    }

    // ── Helper inner handler ──────────────────────────────────────────────────

    private sealed class DelegateHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> _handler;
        public DelegateHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handler)
            => _handler = handler;
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => _handler(request);
    }
}
