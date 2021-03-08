using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Fabron.Contracts;
using FabronService.Commands;
using FabronService.Resources;
using RichardSzalay.MockHttp;
using Xunit;

namespace FabronService.Test.Resourcers
{
    public class APIReminderResourceTests : IClassFixture<WAF<APIReminderResourceTestSiloConfigurator>>
    {
        private readonly WAF<APIReminderResourceTestSiloConfigurator> _waf;
        public APIReminderResourceTests(WAF<APIReminderResourceTestSiloConfigurator> waf)
        {
            _waf = waf;
        }

        [Fact]
        public async Task Create()
        {
            var client = _waf.WithTestUser().CreateClient();
            var request = new CreateAPIReminderResourceRequest(
                "Test_Create",
                DateTime.UtcNow.AddDays(1),
                new RequestWebAPI(
                    "http://localhost",
                    "GET")
                );
            var response = await client.PostAsJsonAsync("/APIReminders", request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal(new Uri(_waf.Server.BaseAddress, $"APIReminders/{request.Name}"), response.Headers.Location);
            var reminder = await response.Content.ReadFromJsonAsync<APIReminderResource>(_waf.JsonSerializerOptions);
            Assert.NotNull(reminder);
            Assert.Equal(request.Command.Url, reminder!.Command.Data.Url);
        }

        [Fact]
        public async Task Get()
        {
            var request = new CreateAPIReminderResourceRequest(
                "Test_Get",
                DateTime.UtcNow,
                new RequestWebAPI(
                    "http://localhost",
                    "GET")
                );
            var client = _waf.WithTestUser().CreateClient();
            var mockHttpHandler = _waf.GetSiloService<MockHttpMessageHandler>();
            mockHttpHandler.When(HttpMethod.Get, request.Command.Url)
                .Respond(HttpStatusCode.OK);
            var response = await client.PostAsJsonAsync("/APIReminders", request);

            var reminder = await client.GetFromJsonAsync<APIReminderResource>(response.Headers.Location, _waf.JsonSerializerOptions);

            Assert.NotNull(reminder);
            Assert.Equal(request.Command.Url, reminder!.Command.Data.Url);
            Assert.Equal(JobStatus.RanToCompletion, reminder!.Status);
            Assert.Equal(200, reminder!.Command.Result);
        }
    }
}
