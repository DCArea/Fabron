using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Orleans.TestingHost;

namespace FabronService.FunctionalTests
{
    public static class WAFExtensions
    {
        public static WebApplicationFactory<Startup> WithTestUser(this WebApplicationFactory<Startup> waf)
        {
            return waf.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                            "Test", options => { });
                });
            });
        }

        public static TestCluster GetSiloCluster(this WebApplicationFactory<Startup> waf)
            => waf.Services.GetRequiredService<TestCluster>();

        public static TService GetSiloService<TService>(this WebApplicationFactory<Startup> waf)
            where TService : notnull
            => ((InProcessSiloHandle)waf.GetSiloCluster().Primary).SiloHost.Services.GetRequiredService<TService>();

        public static Mock<TService> GetSiloServiceProbe<TService>(this WebApplicationFactory<Startup> waf)
            where TService : class
        {
            var service = waf.GetSiloService<TService>();
            return Mock.Get(service);
        }
    }
}
