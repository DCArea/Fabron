
using System.Net.Http;
using System.Threading.Tasks;

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
            HttpClient client = _waf.CreateClient();
            HttpResponseMessage response = await client.GetAsync("/swagger/v1/swagger.json");
            response.EnsureSuccessStatusCode();
            await response.Content.ReadAsStringAsync();
        }

    }

}
