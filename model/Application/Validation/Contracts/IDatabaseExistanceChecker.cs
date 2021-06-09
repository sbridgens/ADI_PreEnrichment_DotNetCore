using System;
using Domain.Entities;
using MappingSchema = Domain.Schema.GNMappingSchema.GnOnApiProgramMappingSchema;
using ProgramSchema = Domain.Schema.GNProgramSchema.GnApiProgramsSchema;

namespace Application.Validation.Contracts
{
    public interface IDatabaseExistenceChecker
    {
        public void EnsureMappingUpdateExist(MappingSchema.onProgramMappingsProgramMapping programMapping, string providerId);
        public void EnsureLayer1UpdateLookupExist(GN_Mapping_Data mapping, ProgramSchema.programsProgram programData);
        public void EnsureLayer2UpdateLookupExist(Guid programGuid, string connectorId, ProgramSchema.programsProgram programData);
    }
}