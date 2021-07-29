using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using DwFramework.Core;
using DwFramework.Quartz;

namespace Core
{
    [Registerable(lifetime: Lifetime.Singleton)]
    public sealed class DDNSService
    {
        public sealed class Config
        {
            public string Corn { get; set; }
            public string Domain { get; set; }
            public string SubDomain { get; set; }
        }

        public sealed class GetIpResult
        {
            [JsonPropertyName("code")]
            public int Code { get; set; }
            [JsonPropertyName("address")]
            public string Address { get; set; }
            [JsonPropertyName("ip")]
            public string Ip { get; set; }
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
                client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36 Edg/91.0.864.67");
                var responseMessage = await client.GetAsync("http://members.3322.org/dyndns/getip");
                if (responseMessage.StatusCode != System.Net.HttpStatusCode.OK) throw new Exception("获取外网IP失败");
                var result = (await responseMessage.Content.ReadAsStringAsync()).Trim('\n');
                if (string.IsNullOrEmpty(result)) throw new Exception("获取外网IP失败");
                var original = await _resolver.UpdateRecordAsync(_config.Domain, _config.SubDomain, result);
                if (original != result) _logger.LogInformation($"域名解析更新成功：{original} => {result}");
                else _logger.LogDebug($"域名解析未更新：{result}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"域名解析更新异常：{ex.Message}");
            }
        }
    }
}