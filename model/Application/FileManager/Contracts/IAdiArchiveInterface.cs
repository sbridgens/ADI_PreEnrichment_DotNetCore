using System.IO;

namespace Application.FileManager.Contracts
{
    public interface IAdiArchiveInterface
    {
        FileInfo ExtractedAdiFile { get; set; }

        FileInfo ExtractedMovieAsset { get; set; }

        FileInfo ExtractedPreview { get; set; }

        bool ExtractItemFromArchive(string sourceArchive, string outputDirectory, bool extractAll, bool extractAdiOnly);

        bool CreateArchive(string sourceDirectory, string destinationArchive);
    }
}