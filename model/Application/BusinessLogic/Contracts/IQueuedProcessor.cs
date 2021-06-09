using System.Threading;
using System.Threading.Tasks;

namespace Application.BusinessLogic.Contracts
{
    public interface IQueuedProcessor
    {
        Task StartAsync(CancellationToken cancellationToken);
    }
}