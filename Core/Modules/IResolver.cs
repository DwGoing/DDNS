using System.Threading.Tasks;

namespace Core
{
    public interface IResolver
    {
        Task<string> GetCurrentIpAsync(string domain, string sudDomain);
    }
}