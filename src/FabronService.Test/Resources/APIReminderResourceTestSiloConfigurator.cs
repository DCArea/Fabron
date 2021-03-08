using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;

namespace FabronService.Test
{
    public class APIReminderResourceTestSiloConfigurator : TestSiloConfigurator
    {
        public override void ConfigureServices(Microsoft.Extensions.Hosting.HostBuilderContext context, IServiceCollection services)
        {
            services.AddSingleton<MockHttpMessageHandler>();
            services.AddSingleton(s => s.GetRequiredService<MockHttpMessageHandler>().ToHttpClient());
        }
    }
}
