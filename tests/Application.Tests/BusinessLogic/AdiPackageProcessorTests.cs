using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Application.BusinessLogic;
using Application.BusinessLogic.Contracts;
using Application.Configuration;
using Application.FileManager.Contracts;
using Application.Models;
using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Application.Tests.BusinessLogic
{
    public class AdiPackageProcessorTests
    {
        private readonly IPackageProcessor _packageProcessor;

        private readonly Mock<IStrategyFactory> _strategyFactory = new Mock<IStrategyFactory>();
        private readonly Mock<IProcessingStrategy> _strategy = new Mock<IProcessingStrategy>();
        private readonly Mock<IPackageImporter> _packageImporter = new Mock<IPackageImporter>();
        private readonly Mock<IFileSystemWorker> _fileSystemWorker = new Mock<IFileSystemWorker>();
        private readonly Mock<IOptions<EnrichmentSettings>> _options = new Mock<IOptions<EnrichmentSettings>>();
        private readonly Mock<ILogger<AdiPackageProcessor>> _logger = new Mock<ILogger<AdiPackageProcessor>>();
        
        private readonly FileInfo _package = new FileInfo("SomePackage.zip");
        private readonly FileInfo _emptyPackage = new FileInfo("EmptyPackage.zip");
        private readonly FileInfo _archive = new FileInfo("Archive");
        private readonly ArchiveInfo _archiveInfo;
        private readonly string _tempFolder;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public AdiPackageProcessorTests()
        {
            _archiveInfo = new ArchiveInfo{Archive = _archive};
            var enrichmentOptions = new EnrichmentSettings {TempWorkingDirectory = Directory.GetCurrentDirectory()};
            _options.Setup(o => o.Value).Returns(enrichmentOptions);
            _packageProcessor = new AdiPackageProcessor(_strategyFactory.Object, 
                _packageImporter.Object, _fileSystemWorker.Object, _options.Object, _logger.Object);
            
            _tempFolder = Path.Combine(_options.Object.Value.TempWorkingDirectory, Path.GetFileNameWithoutExtension(_archive.Name));
            _strategyFactory.Setup(sf => sf.Get(It.IsAny<PackageEntry>())).Returns(_strategy.Object);
        }

        [Fact]
        public async Task ProcessFailedPackage()
        {
            var tempFolder = Path.Combine(_options.Object.Value.TempWorkingDirectory, Path.GetFileNameWithoutExtension(_emptyPackage.Name));
            _packageImporter.Setup(p => p.TryImportPackage(_emptyPackage))
                .Returns(Result.Failure<PackageEntry>("Failed to import package"));
            
            await _packageProcessor.StartAsync(_emptyPackage, _cancellationTokenSource.Token);
            
            _fileSystemWorker.Verify(f => f.MoveToFailedFolder(_emptyPackage), Times.Once);
            _fileSystemWorker.Verify(f => f.RemoveFolder(tempFolder), Times.Once);
        }

        [Fact]
        public async Task ProcessUhdPackageWhenUhdContentIsForbidden()
        {
            var packageEntry = new PackageEntry
            {
                IsUltraHd = true,
                ArchiveInfo = _archiveInfo
            };
            _options.Object.Value.ProcessUHDContent= new ProcessUHDContent
            {
                AllowUHDContentIngest = false
            };
            _packageImporter.Setup(p => p.TryImportPackage(_package))
                .Returns(packageEntry);
            
            await _packageProcessor.StartAsync(_package, _cancellationTokenSource.Token);
            
            _fileSystemWorker.Verify(f => f.MoveToFailedFolder(_archive), Times.Once);
            _fileSystemWorker.Verify(f => f.RemoveFolder(_tempFolder), Times.Once);
        }
        
        [Fact]
        public async Task ProcessAdultPackageWhenAdultEnrichmentIsForbidden()
        {
            var packageEntry = new PackageEntry
            {
                IsAdult = true,
                ArchiveInfo = _archiveInfo,
                FailedToMap = true
            };
            _options.Object.Value.AllowAdultContentIngest = new AllowAdultContentIngest
            {
                AllowAdultEnrichment = false
            };
            _packageImporter.Setup(p => p.TryImportPackage(_package))
                .Returns(packageEntry);
            
            await _packageProcessor.StartAsync(_package, _cancellationTokenSource.Token);
            
            _fileSystemWorker.Verify(f => f.MoveToNonMappedFolder(_archive), Times.Once);
            _fileSystemWorker.Verify(f => f.RemoveFolder(_tempFolder), Times.Once);
        }

        [Fact]
        public async Task ProcessPackageWithFailedStrategy()
        {
            var packageEntry = new PackageEntry { ArchiveInfo = _archiveInfo};
            _packageImporter.Setup(p => p.TryImportPackage(_package))
                .Returns(packageEntry);
            _strategy.Setup(s => s.Execute(packageEntry)).ReturnsAsync(Result.Failure("Failed to process"));
            
            await _packageProcessor.StartAsync(_package, _cancellationTokenSource.Token);
            
            _fileSystemWorker.Verify(f => f.MoveToFailedFolder(_archive), Times.Once);
            _fileSystemWorker.Verify(f => f.RemoveFolder(_tempFolder), Times.Once);
        }

        [Fact]
        public async Task ProcessSuccessfulPackage()
        {
            var packageEntry = new PackageEntry { ArchiveInfo = _archiveInfo};
            _packageImporter.Setup(p => p.TryImportPackage(_package))
                .Returns(packageEntry);
            _strategy.Setup(s => s.Execute(packageEntry)).ReturnsAsync(Result.Success);
            
            await _packageProcessor.StartAsync(_package, _cancellationTokenSource.Token);
            
            _fileSystemWorker.Verify(f => f.DeleteFile(_archive), Times.Once);
            _fileSystemWorker.Verify(f => f.RemoveFolder(_tempFolder), Times.Once);
        }
    }
}