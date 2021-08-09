// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

using Fabron.Contracts;

using FabronService.Resources.HttpReminders;

using Moq;
using Moq.Contrib.HttpClient;

using Xunit;

namespace FabronService.FunctionalTests.Resources
{
    public class HttpReminderTests : IClassFixture<WAF<HttpReminderTestSiloConfigurator>>
    {
        private const string Route = "/HttpReminders";
        private readonly WAF<HttpReminderTestSiloConfigurator> _waf;
        public HttpReminderTests(WAF<HttpReminderTestSiloConfigurator> waf) => _waf = waf;

        [Fact]
        public async Task Create_Get()
        {
            RegisterHttpReminderRequest request = new(
                "Test_Get",
                DateTime.UtcNow,
                new("http://llhh", "GET")
            );
            HttpClient client = _waf.WithTestUser().CreateClient();
            Mock<HttpMessageHandler> handler = _waf.GetSiloService<Mock<HttpMessageHandler>>();
            handler.SetupRequest(HttpMethod.Get, request.Command.Url)
                .ReturnsResponse(HttpStatusCode.OK);

            HttpResponseMessage? response = await client.PostAsJsonAsync(Route, request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal(new Uri(_waf.Server.BaseAddress, $"{Route}/{request.Name}"), response.Headers.Location);
            HttpReminder? reminder = await response.Content.ReadFromJsonAsync<HttpReminder>(_waf.JsonSerializerOptions);
            Assert.NotNull(reminder);
            Assert.Equal(request.Command.Url, reminder!.Command.Data.Url);

            reminder = await client.GetFromJsonAsync<HttpReminder>(response.Headers.Location, _waf.JsonSerializerOptions);

            Assert.NotNull(reminder);
            Assert.Equal(request.Command.Url, reminder!.Command.Data.Url);
            Assert.Equal(JobStatus.Succeed, reminder!.Status);
            Assert.Equal(200, reminder!.Command.Result);
        }
    }
}
