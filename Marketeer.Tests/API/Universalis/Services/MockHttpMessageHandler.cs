using System.Net;

namespace Marketeer.Tests.API.Universalis.Services;

public class MockHttpMessageHandler : HttpMessageHandler {
    private string? responseContent;
    private HttpStatusCode statusCode;

    public MockHttpMessageHandler(string? responseContent, HttpStatusCode statusCode) {
        this.responseContent = responseContent;
        this.statusCode = statusCode;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        var response = new HttpResponseMessage(this.statusCode) {
            Content = this.responseContent != null ? new StringContent(this.responseContent) : null
        };
        return Task.FromResult(response);
    }
}