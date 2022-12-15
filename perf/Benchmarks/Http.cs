using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using Fabron.CloudEvents;
using FabronService.EventRouters;
using Microsoft.Extensions.DependencyInjection;

namespace Benchmarks;

public class HttpBenchmarks
{
    private readonly ServiceProvider _services;
    private readonly MediaTypeHeaderValue _contentType = new("application/cloudevents+json");
    private readonly CloudEventEnvelop _payload;
    private readonly int IterationCount = 10;

    public HttpBenchmarks()
    {
        _services = new ServiceCollection()
            .AddHttpClient()
            .AddLogging()
            .AddSingleton<IHttpDestinationHandler, HttpDestinationHandler>()
            .BuildServiceProvider();
        var data = new
        {
            Foo = "bar"
        };
        _payload = new CloudEventEnvelop(
            Guid.NewGuid().ToString(),
            "testsource",
            "testtype",
            DateTimeOffset.UtcNow,
            JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(data)),
            DataSchema: null,
            Subject: "testsubject"
        );
    }

    [Benchmark]
    public async Task HTTP()
    {
        for (var i = 0; i < IterationCount; i++)
        {
            var client = _services.GetRequiredService<IHttpClientFactory>()
                .CreateClient();
            var destination = new Uri("http://localhost:5000/cloudevents");

            var request = new HttpRequestMessage(HttpMethod.Put, destination)
            {
                Content = JsonContent.Create(_payload, _contentType)
            };
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
    }

    [Benchmark]
    public async Task HTTPS()
    {
        for (var i = 0; i < IterationCount; i++)
        {
            var client = _services.GetRequiredService<IHttpClientFactory>()
                .CreateClient();
            var destination = new Uri("https://localhost:5001/cloudevents");

            var request = new HttpRequestMessage(HttpMethod.Put, destination)
            {
                Content = JsonContent.Create(_payload, _contentType)
            };
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
    }

    [Benchmark]
    public async Task HTTPS_HeaderCompletion()
    {
        for (var i = 0; i < IterationCount; i++)
        {
            var client = _services.GetRequiredService<IHttpClientFactory>()
                .CreateClient();
            var destination = new Uri("https://localhost:5001/cloudevents");

            var request = new HttpRequestMessage(HttpMethod.Put, destination)
            {
                Content = JsonContent.Create(_payload, _contentType)
            };
            var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
        }
    }

    [Benchmark]
    public async Task HTTPS_HeaderCompletion_DisposeResponse()
    {
        for (var i = 0; i < IterationCount; i++)
        {
            using var client = _services.GetRequiredService<IHttpClientFactory>()
                .CreateClient();
            var destination = new Uri("https://localhost:5001/cloudevents");

            using var request = new HttpRequestMessage(HttpMethod.Put, destination)
            {
                Content = JsonContent.Create(_payload, _contentType)
            };
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
        }
    }


}
