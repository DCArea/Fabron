// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Fabron.Mando;

namespace Fabron
{
    public partial class JobManager : IJobManager
    {
        private readonly ILogger _logger;
        private readonly CommandRegistry _registry;
        private readonly IClusterClient _client;
        public JobManager(ILogger<JobManager> logger,
            IOptions<CommandRegistry> options,
            IClusterClient client)
        {
            _logger = logger;
            _registry = options.Value;
            _client = client;
        }

    }
}
