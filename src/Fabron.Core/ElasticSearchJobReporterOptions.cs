// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Fabron
{
    public class ElasticSearchJobReporterOptions
    {
        public string Server { get; set; } = default!;
        public string JobIndexName { get; set; } = default!;
    }
}
