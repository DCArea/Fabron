﻿using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fabron.Dispatching;

namespace FabronService.FireRouters;

public interface IHttpDestinationHandler
{
    Task SendAsync(Uri destination, FireEnvelop envelop);
}

public class HttpDestinationHandler(IHttpClientFactory factory, ILogger<HttpDestinationHandler> logger) : IHttpDestinationHandler
{
    private readonly ILogger _logger = logger;
    private readonly IHttpClientFactory _factory = factory;
    private readonly MediaTypeHeaderValue _contentType = new("application/json");
    private readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task SendAsync(Uri destination, FireEnvelop envelop)
    {
        using var client = _factory.CreateClient();
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Put, destination)
            {
                Version = new Version(2, 0),
                VersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
                Content = JsonContent.Create(envelop, _contentType, _jsonSerializerOptions)
            };
            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to fire timer to {Destination}", destination);
            throw;
        }
    }
}

