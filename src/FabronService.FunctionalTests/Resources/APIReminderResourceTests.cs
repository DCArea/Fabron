// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

using Fabron.Contracts;

using FabronService.Resources;

using Moq;
using Moq.Contrib.HttpClient;

using Xunit;

namespace FabronService.FunctionalTests.Resourcers
{
    public class APIReminderResourceTests : IClassFixture<WAF<APIReminderResourceTestSiloConfigurator>>
    {
        private readonly WAF<APIReminderResourceTestSiloConfigurator> _waf;
        public APIReminderResourceTests(WAF<APIReminderResourceTestSiloConfigurator> waf) => _waf = waf;

        [Fact]
        public async Task Create()
        {
            HttpClient client = _waf.WithTestUser().CreateClient();
            CreateAPIReminderResourceRequest request = new(
                "Test_Create",
                DateTime.UtcNow.AddDays(1),
                new(
                    "http://llhh",
                    "GET")
                );
            HttpResponseMessage response = await client.PostAsJsonAsync("/APIReminders", request);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal(new Uri(_waf.Server.BaseAddress, $"APIReminders/{request.Name}"), response.Headers.Location);
            APIReminderResource? reminder = await response.Content.ReadFromJsonAsync<APIReminderResource>(_waf.JsonSerializerOptions);
            Assert.NotNull(reminder);
            Assert.Equal(request.Command.Url, reminder!.Command.Data.Url);
        }

        [Fact]
        public async Task Get()
        {
            CreateAPIReminderResourceRequest request = new(
                "Test_Get",
                DateTime.UtcNow,
                new(
                    "http://llhh",
                    "GET")
                );
            HttpClient client = _waf.WithTestUser().CreateClient();
            Mock<HttpMessageHandler> handler = _waf.GetSiloService<Mock<HttpMessageHandler>>();
            handler.SetupRequest(HttpMethod.Get, request.Command.Url)
                .ReturnsResponse(HttpStatusCode.OK);

            HttpResponseMessage? response = await client.PostAsJsonAsync("/APIReminders", request);
            APIReminderResource? reminder = await client.GetFromJsonAsync<APIReminderResource>(response.Headers.Location, _waf.JsonSerializerOptions);

            Assert.NotNull(reminder);
            Assert.Equal(request.Command.Url, reminder!.Command.Data.Url);
            Assert.Equal(JobStatus.RanToCompletion, reminder!.Status);
            Assert.Equal(200, reminder!.Command.Result);
        }
    }
}
