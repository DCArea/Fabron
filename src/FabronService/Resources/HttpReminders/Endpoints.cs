﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;

using Fabron;
using Fabron.Contracts;

using FabronService.Commands;
using FabronService.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FabronService.Resources.HttpReminders
{
    [ApiController]
    [Route("HttpReminders")]
    public class Endpoint : ControllerBase
    {
        private readonly ILogger<Endpoint> _logger;
        private readonly IJobManager _jobManager;
        private readonly IResourceLocator _resourceLocator;

        public Endpoint(ILogger<Endpoint> logger,
            IJobManager jobManager,
            IResourceLocator resourceLocator)
        {
            _logger = logger;
            _jobManager = jobManager;
            _resourceLocator = resourceLocator;
        }

        [HttpPost]
        public async Task<IActionResult> Create(RegisterHttpReminderRequest req)
        {
            var tenantId = HttpContext.User.Identity!.Name!;
            var resourceUri = $"tenants/{tenantId}/HttpReminders/{req.Name}";
            var resourceId = await _resourceLocator.GetOrCreateResourceId(resourceUri);
            Job<RequestWebAPI, int>? job = await _jobManager.Schedule<RequestWebAPI, int>(resourceId, req.Command, req.Schedule, new Dictionary<string, string>{ { "tenant", tenantId} });
            HttpReminder reminder = job.ToResource(req.Name);
            return CreatedAtAction(nameof(Get), new { name = reminder.Name }, reminder);
        }

        [HttpGet("{name}")]
        public async Task<IActionResult> Get(string name)
        {
            var tenantId = HttpContext.User.Identity!.Name;
            var resourceUri = $"tenants/{tenantId}/HttpReminders/{name}";
            var resourceId = await _resourceLocator.GetResourceId(resourceUri);
            if (resourceId == null) return NotFound();

            Job<RequestWebAPI, int>? job = await _jobManager.GetJobById<RequestWebAPI, int>(resourceId);
            return job is null ? NotFound() : Ok(job.ToResource(name));
        }

    }
}
