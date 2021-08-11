// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using Prometheus;

using Quartz;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
});
builder.Services.AddHttpClient();
builder.Services.Configure<QuartzOptions>(builder.Configuration.GetSection("Quartz"));
builder.Services.Configure<QuartzOptions>(options =>
{
    options.Scheduling.IgnoreDuplicates = true; // default: false
    options.Scheduling.OverWriteExistingData = true; // default: true
});
builder.Services.AddQuartz(q =>
 {
     // handy when part of cluster or you want to otherwise identify multiple schedulers
     q.SchedulerName = "Scheduler-Core";
     q.SchedulerId = "Scheduler-Core";

     // you can control whether job interruption happens for running jobs when scheduler is shutting down
     q.InterruptJobsOnShutdown = true;

     // when QuartzHostedServiceOptions.WaitForJobsToComplete = true or scheduler.Shutdown(waitForJobsToComplete: true)
     q.InterruptJobsOnShutdownWithWait = true;

     // we can change from the default of 1
     var maxBatchSize = int.Parse(builder.Configuration["MaxBatchSize"]);
     q.MaxBatchSize = maxBatchSize;

     q.UseMicrosoftDependencyInjectionJobFactory();

     // these are the defaults
     q.UseSimpleTypeLoader();
     q.UsePersistentStore(config =>
     {
         config.UsePostgres(builder.Configuration["PgSQLConnectionString"]);
         config.UseJsonSerializer();
     });
     var maxConcurrency = int.Parse(builder.Configuration["MaxConcurrency"]);
     q.UseDefaultThreadPool(maxConcurrency: maxConcurrency);

 });
builder.Services.AddQuartzServer(options =>
{
    // when shutting down we want jobs to complete gracefully
    options.WaitForJobsToComplete = true;
});


var app = builder.Build();

app.UseRouting();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapControllers();
app.MapMetrics();

app.Run();
