using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using DwFramework.Core;

namespace Core
{
    [DisallowConcurrentExecution]
    public sealed class DDNSJob : IJob
    {
        private readonly DDNSService _dDNSService;

        public DDNSJob()
        {
            _dDNSService = ServiceHost.ServiceProvider.GetService<DDNSService>();
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await _dDNSService.UpdateAsync();
        }
    }
}