using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Fabron.Mando;
using System;

namespace FabronService.Commands
{
    public record RequestWebAPI
    (
        string Url,
        string HttpMethod,
        Dictionary<string, string>? Headers = null,
        string? PayloadJson = null
    ) : ICommand<int>;

    public class RequestWebAPIHandler : ICommandHandler<RequestWebAPI, int>
    {
        private readonly HttpClient _client;
        private readonly ILogger _logger;

        public RequestWebAPIHandler(ILogger<RequestWebAPIHandler> logger,
            HttpClient httpClient)
        {
            _client = httpClient;
            _logger = logger;
        }

        public async Task<int> Handle(RequestWebAPI command, CancellationToken token)
        {
            try
            {
                HttpResponseMessage res = await _client.GetAsync(command.Url, token);
                return (int)res.StatusCode;
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Exception on Handling {nameof(RequestWebAPI)}");
                throw;
            }
        }
    }
}
