using System;
using Application.Models;
using CSharpFunctionalExtensions;

namespace Application.BusinessLogic.Contracts
{
    public interface IWorkflowExecutor
    {
        public Result Execute(PackageEntry packageEntry, Guid currentIngestUuid);
        public Result Cleanup();
    }
}