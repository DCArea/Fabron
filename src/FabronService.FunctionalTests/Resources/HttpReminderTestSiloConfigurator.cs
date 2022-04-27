
using System.Net.Http;

using Microsoft.Extensions.DependencyInjection;

using Moq;
using Moq.Contrib.HttpClient;

namespace FabronService.FunctionalTests
{
    public class HttpReminderTestSiloConfigurator : TestSiloConfigurator
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            Mock<HttpMessageHandler>? handlerhMock = new();
            HttpClient? client = handlerhMock.CreateClient();
            services.AddSingleton(handlerhMock);
            services.AddSingleton(client);
        }
    }
}
