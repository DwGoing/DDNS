using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Linq;
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
            [JsonPropertyName("code")]
            public string Code { get; set; }
            [JsonPropertyName("message")]
            public string Message { get; set; }
        }

        public class ResultBase
        {
            [JsonPropertyName("status")]
            public Status Status { get; set; }
        }

        public sealed class Domain
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }
            [JsonPropertyName("status")]
            public string Name { get; set; }
        }

        public sealed class Record
        {
            [JsonPropertyName("id")]
            public string Id { get; set; }
            [JsonPropertyName("name")]
            public string Name { get; set; }
            [JsonPropertyName("type")]
            public string Type { get; set; }
            [JsonPropertyName("value")]
            public string Value { get; set; }
            [JsonPropertyName("status")]
            public string Status { get; set; }
        }

        public sealed class RecordListResult : ResultBase
        {
            [JsonPropertyName("domain")]
            public Domain Domain { get; set; }
            [JsonPropertyName("records")]
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
            Dictionary<string, string> args = new(_commonArgs)
            {
                { "domain", domain },
                { "sub_domain", sudDomain },
                { "record_type", "A" }
            };
            var response = await client.PostAsync("https://dnsapi.cn/Record.List", new FormUrlEncodedContent(args));
            var json = await response.Content.ReadAsStringAsync();
            return json.FromJson<RecordListResult>();
        }

        private async Task<ResultBase> RecordModifyAsync(string domainId, string recordId, string subDomain, string value)
        {
            var client = _httpClientFactory.CreateClient(Guid.NewGuid().ToString());
            client.DefaultRequestHeaders.Add("UserAgent", "DDNS/1.0.0");
            Dictionary<string, string> args = new(_commonArgs)
            {
                { "domain_id", domainId },
                { "record_id", recordId },
                { "sub_domain", subDomain },
                { "record_type", "A" },
                { "record_line", "默认" },
                { "value", value }
            };
            var response = await client.PostAsync("https://dnsapi.cn/Record.Modify", new FormUrlEncodedContent(args));
            var json = await response.Content.ReadAsStringAsync();
            return json.FromJson<ResultBase>();
        }

        public async Task<string> UpdateRecordAsync(string domain, string subDomain, string realIp)
        {
            var listResult = await RecordListAsync(domain, subDomain);
            if (listResult.Status.Code != "1") throw new Exception(listResult.Status.Message);
            var record = listResult.Records.Where(item => item.Status == "enable").FirstOrDefault();
            if (record == null) throw new Exception("未找到可用的解析记录");
            if (record.Value != realIp)
            {
                var modifyResult = await RecordModifyAsync(listResult.Domain.Id, record.Id, subDomain, realIp);
                if (modifyResult.Status.Code != "1") throw new Exception(modifyResult.Status.Message);
            }
            return record.Value;
        }
    }
}