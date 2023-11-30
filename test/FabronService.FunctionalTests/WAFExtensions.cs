using FakeItEasy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Orleans.TestingHost;

namespace FabronService.FunctionalTests
{
    public static class WAFExtensions
    {
        public static TestCluster GetSiloCluster(this WebApplicationFactory<Program> waf)
            => waf.Services.GetRequiredService<TestCluster>();

        public static TService GetSiloService<TService>(this WebApplicationFactory<Program> waf)
            where TService : notnull
            => ((InProcessSiloHandle)waf.GetSiloCluster().Primary).SiloHost.Services.GetRequiredService<TService>();

        public static WebApplicationFactory<Program> WithServices(this WebApplicationFactory<Program> waf, Action<IServiceCollection> configureServices) => waf.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(configureServices);
        });

        public static WebApplicationFactory<Program> WithFakes(this WebApplicationFactory<Program> waf, params object[] fakes) => waf.WithServices(services =>
        {
            foreach (var fake in fakes)
            {
                var fakeType = Fake.GetFakeManager(fake).FakeObjectType;
                services.AddScoped(fakeType, sp => fake);
            }
        });

        public static (HttpClient httpClient, TService fake) CreateClient<TService>(this WebApplicationFactory<Program> waf, WebApplicationFactoryClientOptions clientOptions)
            where TService : class
        {
            var fake = A.Fake<TService>();
            var newWaf = waf.WithFakes(fake);
            var httpClient = newWaf.CreateClient(clientOptions);
            return (httpClient, fake);
        }

        public static (HttpClient httpClient, TService1 fake1, TService2 fake2) CreateClient<TService1, TService2>(this WebApplicationFactory<Program> waf, WebApplicationFactoryClientOptions clientOptions)
            where TService1 : class
            where TService2 : class
        {
            var fake1 = A.Fake<TService1>();
            var fake2 = A.Fake<TService2>();
            var newWaf = waf
                .WithFakes(fake1, fake2);
            var httpClient = newWaf.CreateClient(clientOptions);
            return (httpClient, fake1, fake2);
        }

        public static HttpClient CreateClient(this WebApplicationFactory<Program> waf, WebApplicationFactoryClientOptions clientOptions, params object[] fakes)
        {
            var newWaf = waf
                .WithFakes(fakes);
            var httpClient = newWaf.CreateClient(clientOptions);
            return httpClient;
        }

    }
}
