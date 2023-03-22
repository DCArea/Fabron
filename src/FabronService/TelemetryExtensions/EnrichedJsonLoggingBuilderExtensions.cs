using Microsoft.Extensions.Logging.Console;

namespace FabronService.TelemetryExtensions;

public static class EnrichedJsonLoggingBuilderExtensions
{

    public static ILoggingBuilder AddEnrichedJsonConsole(this ILoggingBuilder builder) =>
        builder.AddEnrichedJsonConsole(null);

    public static ILoggingBuilder AddEnrichedJsonConsole(this ILoggingBuilder builder, Action<EnrichedJsonConsoleFormatterOptions>? configure)
    {
        builder.AddConsole();
        if (configure is not null)
        {
            builder.Services.Configure(configure);
        }
        builder.AddConsoleFormatter<EnrichedJsonConsoleFormatter, EnrichedJsonConsoleFormatterOptions>();
        return builder;
    }

}
