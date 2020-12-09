using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.Threading.Tasks;
using Fabron.CLI.Commands;
using Fabron.CLI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Fabron.CLI
{
    class Program
    {
        static async Task Main(string[] args) => await BuildCommandLine()
            .UseHost(_ => Host.CreateDefaultBuilder(),
                host =>
                {
                    host.ConfigureServices(ConfigureServices);
                })
            .UseDefaults()
            .Build()
            .InvokeAsync(args);

        private static CommandLineBuilder BuildCommandLine()
        {
            var root = new RootCommand(@"$ ");
            root.AddCommand(new CronJob());
            return new CommandLineBuilder(root);
        }

        private static void ConfigureServices(HostBuilderContext _, IServiceCollection services)
        {
            services.AddTransient<CronJobShowHandler>();
            var url = Environment.GetEnvironmentVariable("FABRON_ENDPOINT") ?? throw new ArgumentNullException("endpoint");
            services.AddHttpClient<IRestJobClient, RestJobClient>(client =>
            {
                client.BaseAddress = new Uri(url);
            });
        }
    }
}
