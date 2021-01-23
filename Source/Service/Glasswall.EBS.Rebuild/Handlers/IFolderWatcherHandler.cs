using Microsoft.Extensions.Hosting;
using System;
using System.Threading.Tasks;

namespace Glasswall.EBS.Rebuild.Handlers
{
    public interface IFolderWatcherHandler: IHostedService, IDisposable
    {
    }
}
