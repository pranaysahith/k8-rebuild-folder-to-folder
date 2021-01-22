using Glasswall.EBS.Rebuild.Response;
using System.Net.Http;
using System.Threading.Tasks;

namespace Glasswall.EBS.Rebuild.Handlers
{
    public interface IHttpHandler
    {
        Task<IApiResponse> PostAsync(string url, HttpContent data);
    }
}
