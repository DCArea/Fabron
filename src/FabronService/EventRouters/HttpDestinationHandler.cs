using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Fabron.Core.CloudEvents;
using Microsoft.Extensions.Logging;

namespace FabronService.EventRouters;

public interface IHttpDestinationHandler
{
    Task SendAsync(Uri destination, CloudEventEnvelop envelop);
}

public class HttpDestinationHandler : IHttpDestinationHandler
{
    private readonly ILogger _logger;
    private readonly IHttpClientFactory _factory;
    private readonly MediaTypeHeaderValue _contentType = new("application/cloudevents+json");
    private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
    public HttpDestinationHandler(IHttpClientFactory factory, ILogger<HttpDestinationHandler> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task SendAsync(Uri destination, CloudEventEnvelop envelop)
    {
        var client = _factory.CreateClient();
        HttpResponseMessage response;
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Put, destination)
            {
                Content = JsonContent.Create(envelop, _contentType, _jsonSerializerOptions)
            };
            response = await client.SendAsync(request);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to send event to {Destination}", destination);
            return;
        }
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to send event to {Destination} with status code {StatusCode}", destination, response.StatusCode);
            return;
        }
    }
}

