using System;
using System.IO;

namespace Application.Models
{
    public class PackageEntry
    {
        public AdiData AdiData { get; set; } = new AdiData();
        public GraceNoteData GraceNoteData { get; set; } = new GraceNoteData();
        public ArchiveInfo ArchiveInfo { get; set; } = new ArchiveInfo();
        public Guid IngestUuid { get; set; }
        public string TitlPaIdValue { get; set; }
        public string OnApiProviderId { get; set; }
        public bool IsPackageAnUpdate { get; set; }
        public bool IsQamAsset { get; set; }
        public bool IsSdPackage { get; set; }
        public bool IsAdult { get; set; }
        public bool IsUltraHd { get; set; }
        public bool IsTvodPackage { get; set; }
        public bool HasPreviewAssets { get; set; }
        public bool HasPoster { get; set; }
        public bool IsDuplicateIngest { get; set; }
        public bool UpdateVersionFailure { get; set; }
        public bool FailedToMap { get; set; }
        public string MovieFileSize { get; set; }
        public string MovieChecksum { get; set; }
        public string PreviewFileSize { get; set; }
        public string PreviewFileChecksum { get; set; }
        public bool IsMoviePackage { get; set; }
        public bool IsEpisodeSeries { get; set; }
        public bool PackageIsAOneOffSpecial { get; set; }
        public int SeasonId { get; set; }
        public FileInfo PrimaryAsset { get; set; }
        public FileInfo PreviewAsset { get; set; }
    }
}