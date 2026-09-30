using System.Net;

namespace Marketeer.Tests.API.Universalis.Services;

public class MockHttpMessageHandler : HttpMessageHandler {
    private Queue<(string? Content, HttpStatusCode StatusCode)> responses = new();

    public MockHttpMessageHandler(string? responseContent, HttpStatusCode statusCode) {
        this.responses.Enqueue((responseContent, statusCode));
    }

    public void EnqueueResponse(string? responseContent, HttpStatusCode statusCode) {
        this.responses.Enqueue((responseContent, statusCode));
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
        var (content, statusCode) = this.responses.Count > 1 ? this.responses.Dequeue() : this.responses.Peek();

        var response = new HttpResponseMessage(statusCode) {
            Content = content != null ? new StringContent(content) : null
        };
        return Task.FromResult(response);
    }
}