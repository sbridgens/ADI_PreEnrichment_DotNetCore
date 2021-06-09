using System;
using System.Threading.Tasks;

namespace Application.BusinessLogic.Contracts
{
    public interface IGnMappingComparer
    {
        public bool MappingDataChanged(Guid ingestUuid);
    }
}