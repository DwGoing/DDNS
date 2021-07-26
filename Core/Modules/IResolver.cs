using System.Threading.Tasks;

namespace Core
{
    public interface IResolver
    {
        Task<string> UpdateRecordAsync(string domain, string sudDomain, string realIp);
    }
}