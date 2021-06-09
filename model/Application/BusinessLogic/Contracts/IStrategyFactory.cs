using Application.Models;

namespace Application.BusinessLogic.Contracts
{
    public interface IStrategyFactory
    {
        IProcessingStrategy Get(PackageEntry entry);
    }
}