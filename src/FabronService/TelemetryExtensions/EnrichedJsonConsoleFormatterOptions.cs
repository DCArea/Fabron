// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Encodings.Web;
using System.Text.Json;

namespace Microsoft.Extensions.Logging.Console
{
    /// <summary>
    /// Options for the enriched json console log formatter.
    /// </summary>
    public class EnrichedJsonConsoleFormatterOptions : JsonConsoleFormatterOptions
    {
        public EnrichedJsonConsoleFormatterOptions()
        {
            JsonWriterOptions = new()
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
        }
    }
}
