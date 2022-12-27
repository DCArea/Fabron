using Xunit;

namespace FabronService.FunctionalTests
{
    public class OpenAPITests : IClassFixture<WAF>
    {
        private readonly WAF _waf;

        public OpenAPITests(WAF waf) => _waf = waf;

        [Fact]
        public async Task OpenAPISpec()
        {
            var client = _waf.CreateClient();
            var response = await client.GetAsync("/swagger/v1/swagger.json");
            response.EnsureSuccessStatusCode();
            await response.Content.ReadAsStringAsync();
        }

    }

}
