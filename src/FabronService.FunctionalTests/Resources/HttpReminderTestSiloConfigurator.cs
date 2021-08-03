// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Http;

using Microsoft.Extensions.DependencyInjection;

using Moq;
using Moq.Contrib.HttpClient;

namespace FabronService.FunctionalTests
{
    public class HttpReminderTestSiloConfigurator : TestSiloConfigurator
    {
        public override void ConfigureServices(Microsoft.Extensions.Hosting.HostBuilderContext context, IServiceCollection services)
        {
            Mock<HttpMessageHandler>? handlerhMock = new();
            HttpClient? client = handlerhMock.CreateClient();
            services.AddSingleton(handlerhMock);
            services.AddSingleton(client);
        }
    }
}
