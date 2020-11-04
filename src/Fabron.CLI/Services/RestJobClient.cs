using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Fabron.Contracts;

namespace Fabron.CLI.Services
{
    public interface IRestJobClient
    {
        Task<CronJobDetail?> GetCronJobDetail(string id);
    }

    public class RestJobClient : IRestJobClient
    {
        private readonly HttpClient _client;
        public RestJobClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<CronJobDetail?> GetCronJobDetail(string id)
        {
            var detail = await _client.GetFromJsonAsync<CronJobDetail>($"CronJobs/${id}");
            return detail;
        }

    }

}
