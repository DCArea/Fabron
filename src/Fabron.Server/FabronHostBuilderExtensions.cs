
using System.Collections.Generic;
using System.Reflection;
using Fabron;

namespace Microsoft.Extensions.Hosting
{
    public static class FabronHostBuilderExtensions
    {
        public static FabronServerBuilder UseFabron(this IHostBuilder hostBuilder, IEnumerable<Assembly>? commandAssemblies = null)
        {
            return new FabronServerBuilder(hostBuilder);
        }
    }
}
