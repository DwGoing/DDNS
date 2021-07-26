using System;
using System.Threading.Tasks;
using Quartz;

namespace Core
{
    [DisallowConcurrentExecution]
    public sealed class DDNSJob : IJob
    {
        private readonly DDNSService _dDNSService;

        public DDNSJob(DDNSService dDNSService)
        {
            _dDNSService = dDNSService;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await _dDNSService.UpdateAsync();
        }
    }
}