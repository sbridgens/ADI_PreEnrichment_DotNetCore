using System;

namespace Domain.Entities
{
    public interface IUpdateTracking
    {
        public int Id { get; set; }
        public bool RequiresEnrichment { get; set; }
        public DateTime UpdatesChecked { get; set; }
        public Guid IngestUUID { get; set; }
    }
}