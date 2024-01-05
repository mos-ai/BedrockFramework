using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bedrock.Framework
{
    public static class ServiceCollectionExtensions
    {
        public static IHostBuilder ConfigureServer(this IHostBuilder builder, Action<ServerBuilder> configure)
        {
#if (NETFRAMEWORK || NETSTANDARD2_0 || NETCOREAPP2_0)
            return builder.ConfigureServices((builder, services) =>
#else
            return builder.ConfigureServices(services =>
#endif
            {
                services.AddHostedService<ServerHostedService>();

                services.AddOptions<ServerHostedServiceOptions>()
                        .Configure<IServiceProvider>((options, sp) =>
                        {
                            options.ServerBuilder = new ServerBuilder(sp);
                            configure(options.ServerBuilder);
                        });
            });
        }
    }
}
