using System.Reflection;
using Microsoft.Extensions.Hosting;

namespace Fabron
{
    public static class FabronServerHostBuilderExtensions
    {
        public static FabronClientBuilder UseFabronClient(
            this IHostBuilder hostBuilder,
            IEnumerable<Assembly>? commandAssemblies = null,
            bool cohosted = false) => new(hostBuilder, commandAssemblies, cohosted);
    }
}
