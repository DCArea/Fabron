// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Fabron.Models;

namespace Fabron
{
    public interface IJobEventBus
    {
        Task OnJobStateChanged(Job jobState);

        Task OnCronJobStateChanged(CronJob jobState);

        Task OnJobExecutionFailed(Job jobState, string reason);
    }
}
