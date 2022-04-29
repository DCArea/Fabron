
using Fabron;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Prometheus;

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
                .RegisterCommands();

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseCustomSwagger()
                .UseRouting()
                .UseAuthentication()
                .UseAuthorization()
                .UseEndpoints(endpoints =>
                {
                    endpoints.MapHealthChecks("/health").AllowAnonymous();
                    endpoints.MapMetrics().AllowAnonymous();
                    endpoints.MapCronHttpReminders();
                    endpoints.MapHttpReminders();
                });
        }
    }
}

