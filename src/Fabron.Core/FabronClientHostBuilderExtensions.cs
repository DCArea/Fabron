using Microsoft.Extensions.Hosting;

namespace Fabron
{
    public static class FabronServerHostBuilderExtensions
    {
        public static FabronClientBuilder UseFabronClient(this IHostBuilder hostBuilder, bool cohosted = true)
            => new(hostBuilder, cohosted);
    }
}
