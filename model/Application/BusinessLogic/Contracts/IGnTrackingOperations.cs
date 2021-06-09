using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;

namespace Application.BusinessLogic.Contracts
{
    public interface IGnTrackingOperations
    {
        public Task<Result> StartProcessing(CancellationToken token);
    }
}