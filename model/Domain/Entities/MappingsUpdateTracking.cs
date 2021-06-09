using System;

namespace Domain.Entities
{
    //getters and setters for tables
    public class MappingsUpdateTracking : IUpdateTracking
    {
        public int Id { get; set; }
        public Guid IngestUUID { get; set; }
        public virtual GN_Mapping_Data GnMappingData { get; set; }
        public string GN_ProviderId { get; set; }
        public string Mapping_UpdateId { get; set; }
        public DateTime Mapping_UpdateDate { get; set; }
        public string Mapping_NextUpdateId { get; set; }
        public string Mapping_MaxUpdateId { get; set; }
        public string Mapping_RootId { get; set; }
        public DateTime UpdatesChecked { get; set; }
        public bool RequiresEnrichment { get; set; }
    }
}
