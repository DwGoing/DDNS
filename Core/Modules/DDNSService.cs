using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DwFramework.Core;
using DwFramework.TaskSchedule;

namespace Core
{
    [Registerable(lifetime: Lifetime.Singleton)]
    public sealed class DDNSService
    {
        public class Config
        {
            public string Corn { get; set; }
            public string Domain { get; set; }
        }

        private readonly Config _config;
        private readonly TaskScheduleService _taskScheduleService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IResolver _resolver;
        private readonly ILogger<DDNSService> _logger;
        private string _currentIp = "";

        public DDNSService(IConfiguration configuration, TaskScheduleService taskScheduleService, IHttpClientFactory httpClientFactory, IResolver resolver, ILogger<DDNSService> logger)
        {
            _config = configuration.Get<Config>();
            _taskScheduleService = taskScheduleService;
            _httpClientFactory = httpClientFactory;
            _resolver = resolver;
            _logger = logger;
        }

        public async Task StartAsync()
        {
            _currentIp = await _resolver.GetCurrentIpAsync(_config.Domain, "@");
            var key = Guid.NewGuid().ToString();
            await _taskScheduleService.CreateSchedulerAsync(key);
            await _taskScheduleService.CreateJobAsync<DDNSJob>(key, _config.Corn);
        }

        public async Task UpdateAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient(Guid.NewGuid().ToString());
                var responseMessage = await client.GetAsync("http://members.3322.org/dyndns/getip");
                if (responseMessage.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("获取外网IP失败");
                var ip = await responseMessage.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"域名解析更新异常：{ex.Message}");
            }
        }
    }
}