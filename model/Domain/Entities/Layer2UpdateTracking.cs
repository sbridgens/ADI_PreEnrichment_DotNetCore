using System;

namespace Domain.Entities
{
    public class Layer2UpdateTracking : IUpdateTracking
    {
        public int Id { get; set; }
        public Guid IngestUUID { get; set; }
        public virtual GN_Mapping_Data GnMappingData { get; set; }
        public string GN_Paid { get; set; }
        public string GN_connectorId { get; set; }
        public string Layer2_UpdateId { get; set; }
        public DateTime Layer2_UpdateDate { get; set; }
        public string Layer2_NextUpdateId { get; set; }
        public string Layer2_MaxUpdateId { get; set; }
        public string Layer2_RootId { get; set; }
        public DateTime UpdatesChecked { get; set; }
        public bool RequiresEnrichment { get; set; }
    }
}