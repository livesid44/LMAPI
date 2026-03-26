using System.Net.Http.Headers;
using System.Text;
using LMAPI.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace LMAPI.Tests;

public class LogicMonitorAuthHandlerTests
{
    // ── Authorization header ─────────────────────────────────────────────────

    [Fact]
    public async Task SendAsync_AddsBearerAuthorizationHeader()
    {
        HttpRequestMessage? captured = null;

        var inner = new DelegateHandler(req =>
        {
            captured = req;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        });

        var handler = new LogicMonitorAuthHandler("my-token", NullLogger<LogicMonitorAuthHandler>.Instance)
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
        Assert.Equal("Bearer", captured.Headers.Authorization.Scheme);
        Assert.Equal("my-token", captured.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task SendAsync_AttachesCorrectToken()
    {
        HttpRequestMessage? captured = null;

        var inner = new DelegateHandler(req =>
        {
            captured = req;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        });

        const string token = "lmb_SomeBase64EncodedToken==";
        var handler = new LogicMonitorAuthHandler(token, NullLogger<LogicMonitorAuthHandler>.Instance)
        {
            InnerHandler = inner
        };

        using var client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://test.logicmonitor.com/santaba/rest/")
        };

        await client.GetAsync("device/devices?size=50&offset=0");

        Assert.Equal(token, captured!.Headers.Authorization!.Parameter);
    }

    // ── BuildCurlCommand ─────────────────────────────────────────────────────

    [Fact]
    public void BuildCurlCommand_IncludesMethodAndUrl()
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            new Uri("https://example.logicmonitor.com/santaba/rest/device/devices?size=50&offset=0"));
        request.Headers.Add("Authorization", "Bearer my-token");

        var curl = LogicMonitorAuthHandler.BuildCurlCommand(request, string.Empty);

        Assert.Contains("curl -X GET", curl);
        Assert.Contains("https://example.logicmonitor.com/santaba/rest/device/devices?size=50&offset=0", curl);
    }

    [Fact]
    public void BuildCurlCommand_IncludesBearerAuthorizationHeader()
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            new Uri("https://example.logicmonitor.com/santaba/rest/device/devices"));
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", "my-bearer-token");

        var curl = LogicMonitorAuthHandler.BuildCurlCommand(request, string.Empty);

        Assert.Contains("-H 'Authorization: Bearer my-bearer-token'", curl);
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
