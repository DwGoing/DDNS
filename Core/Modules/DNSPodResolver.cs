using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using DwFramework.Core;

namespace Core
{
    public sealed class DNSPodResolver : IResolver
    {
        public sealed class Config
        {
            public string Id { get; set; }
            public string Token { get; set; }
        }

        public sealed class Status
        {
            public int Code { get; set; }
            public string Message { get; set; }
        }

        public class ResultBase
        {
            public Status Status { get; set; }
        }

        public sealed class Domain
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        public sealed class Record
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }
            public string Value { get; set; }
            public string Status { get; set; }
        }

        public sealed class RecordListResult : ResultBase
        {
            public Domain Domain { get; set; }
            public Record[] Records { get; set; }
        }

        private readonly Config _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private Dictionary<string, string> _commonArgs => new()
        {
            { "login_token", $"{_config.Id},{_config.Token}" },
            { "format", "json" }
        };

        public DNSPodResolver(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _config = configuration.Get<Config>();
            _httpClientFactory = httpClientFactory;
        }

        private async Task<RecordListResult> RecordListAsync(string domain, string sudDomain)
        {
            var client = _httpClientFactory.CreateClient(Guid.NewGuid().ToString());
            client.DefaultRequestHeaders.Add("UserAgent", "DDNS/1.0.0");
            var args = new Dictionary<string, string>(_commonArgs)
            {
                { "domain", domain },
                { "sub_domain", sudDomain }
            };
            var response = await client.PostAsync("https://dnsapi.cn/Record.List", new FormUrlEncodedContent(args));
            var json = await response.Content.ReadAsStringAsync();
            return json.FromJson<RecordListResult>();
        }

        public async Task<string> GetCurrentIpAsync(string domain, string sudDomain)
        {
            try
            {
                var result = await RecordListAsync(domain, sudDomain);
                if (result.Status.Code != 1) throw new Exception(result.Status.Message);
                return null;
            }
            catch
            {
                return "";
            }
        }
    }
}