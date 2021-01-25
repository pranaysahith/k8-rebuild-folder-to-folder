using System.Net.Http;

namespace Glasswall.EBS.Rebuild.Response
{
    public class ApiResponse : IApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public HttpContent Content { get; set; }
    }
}
