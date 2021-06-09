using Application.Models;
using CSharpFunctionalExtensions;

namespace Application.BusinessLogic.Contracts
{
    public interface IImageWorker
    {
        Result ProcessImages(PackageEntry entry);
    }
}