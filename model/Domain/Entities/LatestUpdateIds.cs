using System;

namespace Domain.Entities
{
    public class LatestUpdateIds
    {
        public int Id { get; set; }
        public Int64 LastMappingUpdateIdChecked { get; set; }
        public Int64 LastLayer1UpdateIdChecked { get; set; }
        public Int64 LastLayer2UpdateIdChecked { get; set; }
        public bool InOperation { get; set; }
    }
}
