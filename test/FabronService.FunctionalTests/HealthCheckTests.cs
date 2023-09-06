using Xunit;

namespace FabronService.FunctionalTests
{
    public class HealthCheckTests(WAF waf) : IClassFixture<WAF>
    {
        [Fact]
        public async Task HealthCheck()
        {
            var client = waf.CreateClient();
            var response = await client.GetAsync("/health");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("Healthy", content);
        }

    }

}
