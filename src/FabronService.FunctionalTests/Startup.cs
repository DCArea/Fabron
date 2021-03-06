// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using FabronService.Commands;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FabronService.FunctionalTests
{
    public class TestStartup
    {
        public TestStartup(IConfiguration configuration) => Configuration = configuration;

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
            => services.ConfigureFramework()
                .AddApiKeyAuth(Configuration["ApiKey"])
                .AddSwagger()
                .RegisterJobCommandHandlers();

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            => app.UseCustomSwagger()
                .UseAuthentication()
                .UseRouting()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapHealthChecks("/health").AllowAnonymous();
                    endpoints.MapControllers();
                });
    }
}

