using System;
using System.Threading.Tasks;
using Application.Models;

namespace Application.BusinessLogic.Contracts
{
    public interface IGnLayerDataComprarer
    {
        public Task<bool> ProgramDataChanged(PackageEntry packageEntry, Guid ingestUuid, int layer);
    }
}