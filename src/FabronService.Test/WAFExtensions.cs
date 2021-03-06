using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace FabronService.Test
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
    }
}
