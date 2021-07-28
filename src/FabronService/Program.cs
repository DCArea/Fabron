// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using AspNetCore.Authentication.ApiKey;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

using Orleans.Configuration;
using Orleans.Hosting;

using Prometheus;

IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args)
    .UseFabron((ctx, siloBuilder) =>
    {
        siloBuilder.AddPrometheusTelemetryConsumer()
            .Configure<StatisticsOptions>(options =>
            {
                options.LogWriteInterval = TimeSpan.FromMilliseconds(-1);
            });
        if (ctx.HostingEnvironment.IsEnvironment("Localhost"))
        {
            siloBuilder.UseLocalhostClustering()
                .UseInMemoryJobStore();
        }
        else
        {
            siloBuilder.UseKubernetesHosting()
                .UseRedisClustering(options =>
                {
                    options.ConnectionString = ctx.Configuration["RedisConnectionString"];
                    options.Database = 0;
                })
                .AddRedisGrainStorage("JobStore", options =>
                {
                    options.ConnectionString = ctx.Configuration["RedisConnectionString"];
                    options.UseJson = true;
                    options.DatabaseNumber = 1;
                })
                .UseRedisReminderService(options =>
                {
                    options.ConnectionString = ctx.Configuration["RedisConnectionString"];
                    options.DatabaseNumber = 2;
                });
                //.UseAdoNetReminderService(options =>
                //{
                //    options.Invariant = "Npgsql";
                //    options.ConnectionString = ctx.Configuration["PgSQLConnectionString"];
                //});
        }

    })
    .ConfigureWebHostDefaults(builder =>
    {
        builder
            .ConfigureServices(ConfigureServices)
            .Configure(ConfigureWebApplication);
    });
IHost host = hostBuilder.Build();
await host.RunAsync();


static void ConfigureServices(WebHostBuilderContext context, IServiceCollection services)
{
    services.ConfigureFramework()
        .AddApiKeyAuth(context.Configuration["ApiKey"])
        .AddSwagger();
    services.RegisterJobCommandHandlers();
}

static void ConfigureWebApplication(IApplicationBuilder app)
{
    app.UseCustomSwagger()
        .UseAuthentication()
        .UseRouting()
        .UseEndpoints(endpoints =>
        {
            endpoints.MapHealthChecks("/health").AllowAnonymous();
            endpoints.MapMetrics();
            endpoints.MapControllers();
        });
}


public static class AppConfigureExtensions
{
    public static IServiceCollection ConfigureFramework(this IServiceCollection services)
    {
        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        });
        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });
        services.AddHttpClient();
        services.AddHealthChecks();
        return services;
    }

    public static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "FabronService", Version = "v1" });
        });
        return services;
    }

    public static IServiceCollection AddApiKeyAuth(this IServiceCollection services, Configuration configuration)
    {
        services.AddApiKeyAuth(configuration["ApiKey"]);
        return services;
    }
    public static IServiceCollection AddApiKeyAuth(this IServiceCollection services, string validApiKey)
    {
        services.AddAuthentication(ApiKeyDefaults.AuthenticationScheme)
            .AddApiKeyInAuthorizationHeader(options =>
            {
                options.Realm = "FabronService API";
                options.KeyName = "token";
                options.Events = new ApiKeyEvents
                {
                    OnValidateKey = ctx =>
                    {
                        if (ctx.ApiKey == validApiKey)
                        {
                            ctx.ValidationSucceeded("debug");
                        }
                        else
                        {
                            ctx.ValidationFailed();
                        }
                        return Task.CompletedTask;
                    }
                };
            });
        return services;
    }

    public static IApplicationBuilder UseCustomSwagger(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "FabronService v1"));
        return app;
    }
}
