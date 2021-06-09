using System.Threading.Tasks;
using Application.Models;
using CSharpFunctionalExtensions;

namespace Application.BusinessLogic.Contracts
{
    public interface IProcessingStrategy
    {
        Task<Result> Execute(PackageEntry entry);
    }
}