// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;


namespace FabronService.Resources.HttpReminders
{
    public record RequestWebAPI
    (
        string Url,
        string HttpMethod,
        Dictionary<string, string>? Headers = null,
        string? PayloadJson = null
    );

    public record RegisterHttpReminderRequest
    (
        string Name,
        DateTime Schedule,
        RequestWebAPI Command
    );
}
