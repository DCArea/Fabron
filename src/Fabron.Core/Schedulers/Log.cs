using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Fabron.Schedulers
{
    internal static partial class Log
    {
        [LoggerMessage(
            Level = LogLevel.Warning,
            Message = "[{key}]: Schedule {schedule} is in the past, but still ticking now.")]
        public static partial void TickingForPast(ILogger logger, string key, DateTimeOffset schedule);
    }
}
