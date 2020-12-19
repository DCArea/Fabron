using System;
using Fabron.Server;

namespace Microsoft.Extensions.Hosting
{
    public static class FabronHostBuilderExtensions
    {
        public static IHostBuilder UseFabron(this IHostBuilder hostBuilder, Action<FabronHostBuilder> configureDelegate)
        {
            if (configureDelegate == null)
            {
                throw new ArgumentNullException(nameof(configureDelegate));
            }
            FabronHostBuilder fabronBuilder;
            if (!hostBuilder.Properties.ContainsKey("FabronBuilder"))
            {
                fabronBuilder = new FabronHostBuilder(hostBuilder);
                hostBuilder.Properties.Add("FabronBuilder", fabronBuilder);
            }
            else
            {
                fabronBuilder = (FabronHostBuilder)hostBuilder.Properties["FabronBuilder"];
            }
            configureDelegate(fabronBuilder);
            return hostBuilder;
        }
    }
}
