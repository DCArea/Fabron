using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.Contrib.HttpClient;

namespace FabronService.FunctionalTests
{
    public class APIReminderResourceTestSiloConfigurator : TestSiloConfigurator
    {
        public override void ConfigureServices(Microsoft.Extensions.Hosting.HostBuilderContext context, IServiceCollection services)
        {
            var handlerhMock = new Mock<HttpMessageHandler>();
            var client = handlerhMock.CreateClient();
            services.AddSingleton(handlerhMock);
            services.AddSingleton(client);
        }
    }
}
