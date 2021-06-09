using System;

namespace Domain.Entities
{
    public class GN_Api_Lookup
    {
        public int Id { get; set; }
        public Guid IngestUUID { get; set; }
        public virtual GN_Mapping_Data GnMappingData { get; set; }
        public string GN_TMSID { get; set; }
        public string GN_connectorId { get; set; }
        public string GnMapData { get; set; }
        public string GnLayer1Data { get; set; }
        public string GnLayer2Data { get; set; }
    }
}
