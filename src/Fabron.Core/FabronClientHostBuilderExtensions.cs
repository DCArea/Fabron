
using System.Collections.Generic;
using System.Reflection;
using Fabron;

namespace Microsoft.Extensions.Hosting
{
    public static class FabronServerHostBuilderExtensions
    {
        public static FabronClientBuilder UseFabronClient(
            this IHostBuilder hostBuilder,
            IEnumerable<Assembly>? commandAssemblies = null,
            bool cohosted = false)
        {
            return new FabronClientBuilder(hostBuilder, commandAssemblies, cohosted);
        }
    }
}
