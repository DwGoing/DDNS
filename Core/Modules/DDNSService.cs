using System;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DwFramework.Core;
using DwFramework.Quartz;

namespace Core
{
    [Registerable(lifetime: Lifetime.Singleton)]
    public sealed class DDNSService
    {
        public class Config
        {
            public string Corn { get; set; }
            public string Domain { get; set; }
            public string SubDomain { get; set; }
        }

        private readonly Config _config;
        private readonly QuartzService _quartzService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IResolver _resolver;
        private readonly ILogger<DDNSService> _logger;

        public DDNSService(IConfiguration configuration, QuartzService quartzService, IHttpClientFactory httpClientFactory, IResolver resolver, ILogger<DDNSService> logger)
        {
            _config = configuration.Get<Config>();
            _quartzService = quartzService;
            _httpClientFactory = httpClientFactory;
            _resolver = resolver;
            _logger = logger;
        }

        public async Task StartAsync()
        {
            var key = Guid.NewGuid().ToString();
            var scheduler = await _quartzService.CreateSchedulerAsync(key, true);
            await _quartzService.CreateJobAsync<DDNSJob>(key, _config.Corn);
            await scheduler.Start();
        }

        public async Task UpdateAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient(Guid.NewGuid().ToString());
                var responseMessage = await client.GetAsync("http://members.3322.org/dyndns/getip");
                if (responseMessage.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("获取外网IP失败");
                var ip = (await responseMessage.Content.ReadAsStringAsync()).Trim('\n');
                var original = await _resolver.UpdateRecordAsync(_config.Domain, _config.SubDomain, ip);
                if (original != ip) _logger.LogInformation($"域名解析更新成功：{original} => {ip}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"域名解析更新异常：{ex.Message}");
            }
        }
    }
}