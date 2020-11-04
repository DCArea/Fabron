using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Fabron.CLI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
// using Microsoft.Extensions.Hosting;

namespace Fabron.CLI.Commands
{
    public record CronJobShowOptions(
        string Id
    );
    public class CronJobShow : Command
    {
        public CronJobShow()
            : base("show")
        {
            var jobId = new Option<string>("--id")
            {
                Name = "job id",
                IsRequired = true
            };
            AddOption(jobId);
            Handler = CommandHandler.Create<CronJobShowOptions, IHost>(async (CronJobShowOptions options, IHost host)
                => await host.Services.GetRequiredService<CronJobShowHandler>()
                .HandleAsync(options));
        }
    }

    public class CronJobShowHandler
    {
        private readonly IRestJobClient _client;
        public CronJobShowHandler(IRestJobClient client)
        {
            _client = client;
        }

        public async Task<int> HandleAsync(CronJobShowOptions options)
        {
            var detail = await _client.GetCronJobDetail(options.Id);

            return 0;
        }

    }
}
