using System.Net.Http.Json;

namespace Cobryx.IntegrationTests.Helpers;

public static class HttpClientExtensions
{
    public static async Task<HttpResponseMessage> PostIdempotentAsync(this HttpClient client, string url, object content)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(content)
        };
        
        if (!request.Headers.Contains("X-Idempotency-Key"))
        {
            request.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString());
        }
        
        return await client.SendAsync(request);
    }

    public static async Task<HttpResponseMessage> PostIdempotentAsync(this HttpClient client, string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        
        if (!request.Headers.Contains("X-Idempotency-Key"))
        {
            request.Headers.Add("X-Idempotency-Key", Guid.NewGuid().ToString());
        }
        
        return await client.SendAsync(request);
    }
}
