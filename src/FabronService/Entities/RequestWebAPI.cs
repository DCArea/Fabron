using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Fabron.Mando;

namespace Fabron.Server.Entities
{
    public record RequestWebAPI
    (
        string Url,
        string HttpMethod,
        Dictionary<string, string>? Headers,
        string? PayloadJson
    ) : ICommand<int>;

    public class RequestWebAPIHandler : ICommandHandler<RequestWebAPI, int>
    {
        private readonly HttpClient _client;
        private readonly ILogger _logger;

        public RequestWebAPIHandler(ILogger<RequestWebAPIHandler> logger,
            IHttpClientFactory httpClientFactory)
        {
            _client = httpClientFactory.CreateClient();
            _logger = logger;
        }

        public async Task<int> Handle(RequestWebAPI command, CancellationToken token)
        {
            HttpResponseMessage res = await _client.GetAsync(command.Url, token);
            return (int)res.StatusCode;
        }
    }
}
