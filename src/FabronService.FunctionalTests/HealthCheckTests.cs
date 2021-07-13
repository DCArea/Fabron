// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

using Xunit;

namespace FabronService.FunctionalTests
{
    public class HealthCheckTests : IClassFixture<WAF>
    {
        private readonly WAF _waf;

        public HealthCheckTests(WAF waf) => _waf = waf;

        [Fact]
        public async Task HealthCheck()
        {
            System.Net.Http.HttpClient? client = _waf.CreateClient();
            System.Net.Http.HttpResponseMessage? response = await client.GetAsync("/health");
            response.EnsureSuccessStatusCode();
            string? content = await response.Content.ReadAsStringAsync();
            Assert.Equal("Healthy", content);
        }

    }

}
