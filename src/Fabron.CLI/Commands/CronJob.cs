using System;
using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;

namespace Fabron.CLI.Commands
{
    public class CronJob : Command
    {
        public CronJob()
            : base("cron")
        {
            AddCommand(new CronJobShow());
        }

    }
}
