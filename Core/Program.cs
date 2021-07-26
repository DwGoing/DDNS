using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using DwFramework.Core;
using DwFramework.Quartz;

namespace Core
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (!Enum.TryParse<ResolverType>(Environment.GetEnvironmentVariable("RESOLVER_TYPE"), true, out var resolverType))
                throw new Exception("未定义的解析器类型");
            var host = new ServiceHost(environmentType: Environment.GetEnvironmentVariable("ENVIRONMENT_TYPE"), args: args);
            host.ConfigureLogging(builder => builder.UserNLog());
            host.AddJsonConfiguration("Config.json");
            host.ConfigureQuartz();
            host.ConfigureServices(services =>
            {
                services.AddSingleton<DDNSJob>();
                services.AddHttpClient();
                switch (resolverType)
                {
                    case ResolverType.DNSPod:
                        services.AddSingleton<IResolver, DNSPodResolver>();
                        break;
                }
            });
            host.RegisterFromAssemblies();
            host.OnHostStarted += async p =>
            {
                await p.GetService<DDNSService>().StartAsync();
            };
            await host.RunAsync();
        }
    }
}
