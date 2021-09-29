// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Fabron;
using Fabron.Events;
using Fabron.TestRunner.Scenarios;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

var host = await Host.CreateDefaultBuilder()
    .ConfigureHostConfiguration(config =>
    {
        config.AddInMemoryCollection(new Dictionary<string, string>
            {
                { "Logging:LogLevel:Default", "Error" },
                // { "Logging:LogLevel:Fabron.Grains.CronJobGrain", "Debug" },
                { "Logging:LogLevel:Fabron", "Debug" }
            });
    })
    .ConfigureServices(services =>
    {
        services.AddFabron();
    })
    .UseFabron((context, silo) =>
    {
        silo
            .Configure<CronJobOptions>(options => options.UseAsynchronousIndexer = false)
            .Configure<StatisticsOptions>(options =>
            {
                options.LogWriteInterval = TimeSpan.FromMilliseconds(-1);
            })
            .UseLocalhostClustering()
            .UseInMemory();
    }
    )
    .UseConsoleLifetime()
    .StartAsync();
// .RunConsoleAsync();

// await new ScheduleCronJob().PlayAsync();
// await new LabelBasedQuery().PlayAsync();
await new TimerReentrantConflict().PlayAsync();
