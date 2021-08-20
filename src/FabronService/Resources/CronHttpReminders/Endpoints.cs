// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System.Collections.Generic;
using System.Threading.Tasks;
using Fabron;
using FabronService.Commands;
using FabronService.Resources.CronHttpReminders.Models;
using FabronService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FabronService.Resources.CronHttpReminders
{
    [ApiController]
    [Route("CronHttpReminders")]
    public class Endpoints : ControllerBase
    {
        private readonly ILogger<Endpoints> _logger;
        private readonly IJobManager _jobManager;
        private readonly IResourceLocator _resourceLocator;

        public Endpoints(ILogger<Endpoints> logger,
            IJobManager jobManager,
            IResourceLocator resourceLocator)
        {
            _logger = logger;
            _jobManager = jobManager;
            _resourceLocator = resourceLocator;
        }

        [HttpPost(Name = "CronHttpReminders_Register")]
        public async Task<IActionResult> Create(RegisterCronHttpReminderRequest req)
        {
            var tenantId = HttpContext.User.Identity!.Name!;
            var resourceUri = $"tenants/{tenantId}/CronHttpReminders/{req.Name}";
            var resourceId = await _resourceLocator.GetOrCreateResourceId(resourceUri);
            var job = await _jobManager.Schedule<RequestWebAPI>(resourceId, req.Schedule, req.Command, new Dictionary<string, string> { { "tenant", tenantId } });
            var reminder = job.ToResource(req.Name);
            return CreatedAtRoute("CronHttpReminders_Get", new { name = reminder.Name }, reminder);
        }

        [HttpGet("{name}", Name = "CronHttpReminders_Get")]
        public async Task<IActionResult> Get(string name)
        {
            var tenantId = HttpContext.User.Identity!.Name;
            var resourceUri = $"tenants/{tenantId}/CronHttpReminders/{name}";
            var resourceId = await _resourceLocator.GetResourceId(resourceUri);
            if (resourceId == null) return NotFound();

            var job = await _jobManager.GetCronJob<RequestWebAPI>(resourceId);
            return job is null ? NotFound() : Ok(job.ToResource(name));
        }

    }
}
