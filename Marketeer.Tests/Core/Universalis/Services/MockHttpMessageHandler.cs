using System.Net;
using System.Text;

namespace Marketeer.Tests.Core.Universalis.Services;

public class MockHttpMessageHandler : HttpMessageHandler {
    private string responseContent;
    private HttpStatusCode statusCode;

    public MockHttpMessageHandler(string responseContent, HttpStatusCode statusCode) {
        this.responseContent = responseContent;
        this.statusCode = statusCode;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        return Task.FromResult(new HttpResponseMessage {
            StatusCode = this.statusCode,
            Content = new StringContent(this.responseContent, Encoding.UTF8, "application/json")
        });
    }
}