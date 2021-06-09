using System.IO;

namespace Application.Models
{
    public class ArchiveInfo
    {
        public bool HasMediaFolder { get; set; }
        public bool HasPreviewAssets { get; set; }
        public FileInfo Archive { get; set; }
        public DirectoryInfo WorkingDirectory { get; set; }
        public bool AdiExtracted { get; set; }
        public bool IsLegacyGoPackage { get; set; }
        public bool StlExtracted { get; set; }
        public bool MovieAssetExtracted { get; set; }
        public bool PreviewExtracted { get; set; }
        public bool PreviewOnly { get; set; }
        public FileInfo ExtractedAdiFile { get; set; }
        public FileInfo ExtractedMovieAsset { get; set; }
        public FileInfo ExtractedPreview { get; set; }
        public FileInfo ExtractedSubtitle { get; set; }
        public string DeliveryPackage { get; set; }
    }
}