using System.Threading.Tasks;
using Xunit;

namespace FabronService.Test
{
    public class HealthCheckTests : IClassFixture<WAF>
    {
        private readonly WAF _waf;

        public HealthCheckTests(WAF waf)
        {
            _waf = waf;
        }

        [Fact]
        public async Task HealthCheck()
        {
            var client = _waf.CreateClient();
            var response = await client.GetAsync("/health");
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            Assert.Equal("Healthy", content);
        }

    }

}
