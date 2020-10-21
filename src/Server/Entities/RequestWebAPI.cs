using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TGH.Contracts;

namespace TGH.Server.Entities
{
    public record RequestWebAPICommand
    (
        string Url,
        string HttpMethod,
        Dictionary<string, string>? Headers,
        string? PayloadJson
    ) : ICommand<int>;

    public class RequestWebAPICommandHandler : ICommandHandler<RequestWebAPICommand, int>
    {
        private readonly HttpClient _client;
        private readonly ILogger _logger;

        public RequestWebAPICommandHandler(ILogger<RequestWebAPICommandHandler> logger,
            IHttpClientFactory httpClientFactory)
        {
            _client = httpClientFactory.CreateClient();
            _logger = logger;
        }

        public async Task<int> Handle(RequestWebAPICommand command, CancellationToken token)
        {
            var res = await _client.GetAsync(command.Url, token);
            return (int)res.StatusCode;
        }

        public async Task<string> Handle(string data, CancellationToken token)
        {
            var typedCommand = JsonSerializer.Deserialize<RequestWebAPICommand>(data);
            var result = await Handle(typedCommand!, token);
            return JsonSerializer.Serialize<int>(result);
        }
    }
}
