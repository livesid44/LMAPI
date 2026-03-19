using System.Text;
using LMAPI.Infrastructure;

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

        var handler = new LogicMonitorAuthHandler("test-id", "test-key")
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
