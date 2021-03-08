using System;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using FabronService.Commands;
using FabronService.Resources;
using Xunit;

namespace FabronService.Test.Resourcers
{
    public class APIReminderResourceTests : IClassFixture<WAF>
    {
        private readonly WAF _waf;
        public APIReminderResourceTests(WAF waf)
        {
            _waf = waf;
        }

        [Fact]
        public async Task Create()
        {
            var client = _waf.WithTestUser().CreateClient();
            var request = new CreateAPIReminderResourceRequest(
                "TestReminder123",
                DateTime.Now.AddDays(1),
                new RequestWebAPI(
                    "http://localhost",
                    "GET")
                );
            var response = await client.PostAsJsonAsync("/APIReminders", request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal(new Uri(_waf.Server.BaseAddress, "APIReminders/TestReminder123"), response.Headers.Location);
            var reminder = await response.Content.ReadFromJsonAsync<APIReminderResource>(_waf.JsonSerializerOptions);
            Assert.NotNull(reminder);
            Assert.Equal(request.Command.Url, reminder!.Command.Data.Url);
        }

        [Fact]
        public async Task Get()
        {
            var client = _waf.WithTestUser().CreateClient();
            var request = new CreateAPIReminderResourceRequest(
                "TestReminder123",
                DateTime.Now.AddDays(1),
                new RequestWebAPI(
                    "http://localhost",
                    "GET")
                );
            var response = await client.PostAsJsonAsync("/APIReminders", request);

            var reminder = await client.GetFromJsonAsync<APIReminderResource>(response.Headers.Location, _waf.JsonSerializerOptions);

            Assert.NotNull(reminder);
            Assert.Equal(request.Command.Url, reminder!.Command.Data.Url);
        }
    }
}
