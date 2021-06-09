using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Application.BusinessLogic.Contracts
{
    public interface IPackageProcessor
    {
        Task StartAsync(FileInfo package, CancellationToken cancellationToken);
    }
}