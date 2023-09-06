using Xunit;

namespace FabronService.FunctionalTests
{
    public class OpenAPITests(WAF waf) : IClassFixture<WAF>
    {
        [Fact]
        public async Task OpenAPISpec()
        {
            var client = waf.CreateClient();
            var response = await client.GetAsync("/swagger/v1/swagger.json");
            response.EnsureSuccessStatusCode();
            await response.Content.ReadAsStringAsync();
        }

    }

}
