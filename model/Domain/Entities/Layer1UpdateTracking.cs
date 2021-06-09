using System;

namespace Domain.Entities
{
    public class Layer1UpdateTracking: IUpdateTracking
    {
        public int Id { get; set; }
        public Guid IngestUUID { get; set; }
        public virtual GN_Mapping_Data GnMappingData { get; set; }
        public string GN_Paid { get; set; }
        public string GN_TMSID { get; set; }
        public string Layer1_UpdateId { get; set; }
        public DateTime Layer1_UpdateDate { get; set; }
        public string Layer1_NextUpdateId { get; set; }
        public string Layer1_MaxUpdateId { get; set; }
        public string Layer1_RootId { get; set; }
        public DateTime UpdatesChecked { get; set; }
        public bool RequiresEnrichment { get; set; }
    }
}