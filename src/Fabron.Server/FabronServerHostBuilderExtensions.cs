using System.Reflection;
using Microsoft.Extensions.Hosting;

namespace Fabron.Server
{
    public static class FabronServerHostBuilderExtensions
    {
        public static FabronServerBuilder UseFabronServer(this IHostBuilder hostBuilder, IEnumerable<Assembly>? commandAssemblies = null) => new(hostBuilder);
    }
}
