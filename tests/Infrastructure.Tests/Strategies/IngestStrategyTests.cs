using System.Threading.Tasks;
using Application.BusinessLogic.Contracts;
using Application.Configuration;
using Application.DataAccess.Persistence.Contracts;
using Application.FileManager.Contracts;
using Application.Models;
using Application.Validation.Contracts;
using AutoFixture;
using Infrastructure.Strategies;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Infrastructure.Tests.Strategies
{
    public class IngestStrategyTests
    {
        private readonly Fixture _fixture = new Fixture();
        
        private readonly Mock<IApplicationDbContext> _context = new Mock<IApplicationDbContext>();
        private readonly Mock<IFileSystemWorker> _fileSystemWorker = new Mock<IFileSystemWorker>();
        private readonly Mock<IGraceNoteMetadataProvider> _gnMetadataProvider = new Mock<IGraceNoteMetadataProvider>();
        private readonly Mock<IZipArchiveWorker> _zipArchiveWorker = new Mock<IZipArchiveWorker>();
        private readonly Mock<ISystemClock> _systemClock = new Mock<ISystemClock>();
        private readonly Mock<IPackageValidator> _packageValidator = new Mock<IPackageValidator>();
        private readonly Mock<IMetadataMapper> _metadataMapper = new Mock<IMetadataMapper>();
        private readonly Mock<IGnMappingDataStore> _gnMappingDataStore = new Mock<IGnMappingDataStore>();
        private readonly Mock<IImageWorker> _imageWorker = new Mock<IImageWorker>();
        private readonly Mock<IOptions<EnrichmentSettings>> _optionsWrapper = new Mock<IOptions<EnrichmentSettings>>();
        private readonly EnrichmentSettings _options = new EnrichmentSettings();
        private readonly IngestStrategy _sut;

        public IngestStrategyTests()
        {
            _optionsWrapper = new Mock<IOptions<EnrichmentSettings>>();
            _optionsWrapper.Setup(options => options.Value).Returns(_options);

            _sut = new IngestStrategy(_context.Object,
                _fileSystemWorker.Object,
                _gnMetadataProvider.Object,
                _zipArchiveWorker.Object,
                _systemClock.Object,
                _packageValidator.Object,
                _metadataMapper.Object,
                _gnMappingDataStore.Object,
                _imageWorker.Object,
                NullLoggerFactory.Instance,
                _optionsWrapper.Object
            );
        }
        
        [Fact]
        public async Task Test1()
        {
            var testPackage = new PackageEntry();
            await _sut.Execute(testPackage);
        }
    }
}