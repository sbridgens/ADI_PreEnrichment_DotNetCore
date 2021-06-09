using System.Collections.Generic;
using System.IO;
using Application.BusinessLogic;
using Application.BusinessLogic.Contracts;
using Application.Configuration;
using Application.FileManager.Contracts;
using Application.FileManager.Serialization;
using Application.Models;
using Application.Validation.Contracts;
using CSharpFunctionalExtensions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Application.Tests.BusinessLogic
{
    public class PackageImporterTests
    {
        private readonly IPackageImporter _packageImporter;
        
        private readonly Mock<IZipArchiveWorker> _zipWorker = new Mock<IZipArchiveWorker>();
        private readonly Mock<IAdiImporter> _importer = new Mock<IAdiImporter>();
        private readonly Mock<IAdiValidator> _validator = new Mock<IAdiValidator>();
        private readonly Mock<IFileSystemWorker> _fileSystemWorker = new Mock<IFileSystemWorker>();
        private readonly Mock<IOptions<EnrichmentSettings>> _options = new Mock<IOptions<EnrichmentSettings>>();

        private const string AssetId = "SomeAdi";
        private readonly FileInfo _package = new FileInfo("SomePackage.zip");
        private readonly FileInfo _emptyPackage = new FileInfo("EmptyPackage.zip");

        public PackageImporterTests()
        {
            var enrichmentOptions = new EnrichmentSettings {TempWorkingDirectory = Directory.GetCurrentDirectory()};
            _options.Setup(o => o.Value).Returns(enrichmentOptions);
            
            _packageImporter = new PackageImporter(_zipWorker.Object, _importer.Object, _validator.Object, 
                _options.Object, new NullLogger<PackageImporter>());
            
            // Default setups
            _zipWorker.Setup(z => z.ExtractAdiXml(_package, It.IsAny<string>()))
                .Returns(Maybe<FileInfo>.From(_package));
            _validator.Setup(v => v.ValidatePaidValue(It.IsAny<ADI>())).Returns(new AdiValidationResult());
            _zipWorker.Setup(z => z.ReadArchiveInfo(It.IsAny<FileInfo>())).Returns(new ArchiveInfo());
        }

        [Fact]
        public void ImportNonExistingPackage()
        {
            _zipWorker.Setup(z => z.ExtractAdiXml(_emptyPackage, It.IsAny<string>()))
                .Returns(Maybe<FileInfo>.None);

            var result = _packageImporter.TryImportPackage(_emptyPackage);

            result.IsFailure.Should().BeTrue();
            result.Error.Should().Be($"Could not find ADI.xml file in package {_emptyPackage.FullName}.");
        }

        /*
        [Fact]
        public void ImportFailedXmlPackage()
        {
            _importer.Setup(i => i.ReadFromXml(It.IsAny<string>())).Returns(Result.Failure<ADI>("Failed xml read"));
            
            var result = _packageImporter.TryImportPackage(_package);

            result.IsFailure.Should().BeTrue();
            result.Error.Should().StartWith($"Failed to import ADI definition from XML file:");
        }*/

        [Fact]
        public void ImportDefaultPackage()
        {
            var adi = CreateAdi();
            _importer.Setup(i => i.ReadFromXml(It.IsAny<string>())).Returns(adi);
            
            var importResult = _packageImporter.TryImportPackage(_package);
            var packageEntry = importResult.Value;

            importResult.IsSuccess.Should().BeTrue();
            packageEntry.IsPackageAnUpdate.Should().BeTrue();
            packageEntry.IsSdPackage.Should().BeTrue();
            packageEntry.IsAdult.Should().BeFalse();
            packageEntry.IsUltraHd.Should().BeFalse();
            packageEntry.HasPoster.Should().BeFalse();
            packageEntry.IsTvodPackage.Should().BeFalse();
            packageEntry.HasPreviewAssets.Should().BeFalse();
        }

        [Fact]
        public void ImportNonUpdatePackage()
        {
            var adi = CreateAdi();
            var archiveInfo = new ArchiveInfo
            {
                HasMediaFolder = true
            };
            _zipWorker.Setup(z => z.ReadArchiveInfo(_package)).Returns(archiveInfo);
            _importer.Setup(i => i.ReadFromXml(It.IsAny<string>())).Returns(adi);
            
            var packageEntry = _packageImporter.TryImportPackage(_package);

            packageEntry.IsSuccess.Should().BeTrue();
            packageEntry.Value.IsPackageAnUpdate.Should().BeFalse();
        }

        [Fact]
        public void ImportSdPackage()
        {
            var adi = CreateAdi(isSdAsset: true);
            var archiveInfo = new ArchiveInfo
            {
                HasMediaFolder = true
            };
            _options.Object.Value.AllowSDContentIngest = "True";
            _zipWorker.Setup(z => z.ReadArchiveInfo(_package)).Returns(archiveInfo);
            _importer.Setup(i => i.ReadFromXml(It.IsAny<string>())).Returns(adi);
            
            var packageEntry = _packageImporter.TryImportPackage(_package);

            packageEntry.IsSuccess.Should().BeTrue();
            packageEntry.Value.IsSdPackage.Should().BeTrue();
        }

        [Fact]
        public void ImportAdultPackage()
        {
            var adi = CreateAdi(isAdultContent: true);
            _importer.Setup(i => i.ReadFromXml(It.IsAny<string>())).Returns(adi);
            
            var packageEntry = _packageImporter.TryImportPackage(_package);

            packageEntry.IsSuccess.Should().BeTrue();
            packageEntry.Value.IsAdult.Should().BeTrue();
        }
        
        [Fact]
        public void ImportUltraHdPackage()
        {
            var adi = CreateAdi(isUltraHdAsset: true);
            _importer.Setup(i => i.ReadFromXml(It.IsAny<string>())).Returns(adi);
            
            var packageEntry = _packageImporter.TryImportPackage(_package);

            packageEntry.IsSuccess.Should().BeTrue();
            packageEntry.Value.IsUltraHd.Should().BeTrue();
        }
        
        [Fact]
        public void ImportPosterPackage()
        {
            var adi = CreateAdi(isPosterAsset: true);
            _importer.Setup(i => i.ReadFromXml(It.IsAny<string>())).Returns(adi);
            
            var packageEntry = _packageImporter.TryImportPackage(_package);

            packageEntry.IsSuccess.Should().BeTrue();
            packageEntry.Value.HasPoster.Should().BeTrue();
        }
        
        [Fact]
        public void ImportTvodPackage()
        {
            var adi = CreateAdi(isTvodAsset: true);
            _importer.Setup(i => i.ReadFromXml(It.IsAny<string>())).Returns(adi);
            
            var packageEntry = _packageImporter.TryImportPackage(_package);

            packageEntry.IsSuccess.Should().BeTrue();
            packageEntry.Value.IsTvodPackage.Should().BeTrue();
        }
        
        [Fact]
        public void ImportPreviewPackage()
        {
            var adi = CreateAdi(isPreviewAsset: true);
            _importer.Setup(i => i.ReadFromXml(It.IsAny<string>())).Returns(adi);

            var packageEntry = _packageImporter.TryImportPackage(_package);

            packageEntry.IsSuccess.Should().BeTrue();
            packageEntry.Value.HasPreviewAssets.Should().BeTrue();
        }

        private static Result<ADI> CreateAdi(bool isTvodAsset = false, bool isPreviewAsset = false, bool isUltraHdAsset = false, bool isAdultContent = false,
            bool isPosterAsset = false, bool isSdAsset = false)
        {
            var adi = new ADI
            {
                Asset = new ADIAsset
                {
                    Asset = new List<ADIAssetAsset>
                    {
                        CreateMainAsset(isTvodAsset, isPreviewAsset, isUltraHdAsset, isSdAsset)
                    },
                    Metadata = CreateAdiAssetMetadata(AssetId, isAdultContent)
                }
            };
            
            if (isPosterAsset)
                adi.Asset.Asset.Add(CreatePosterAsset());

            return adi;
        }

        private static ADIAssetAsset CreateMainAsset(bool isTvodAsset, bool isPreviewAsset, bool isUltraHdAsset, bool isSdAsset)
        {
            var asset = new ADIAssetAsset
            {
                Content = new ADIAssetAssetContent
                {
                    Value = "Content"
                },
                Metadata = new ADIAssetAssetMetadata
                {
                    AMS = new ADIAssetAssetMetadataAMS
                    {
                        Asset_ID = AssetId,
                    }
                }
            };

            asset.Metadata.AMS.Product = isTvodAsset ? "Tvod" : "SomeProduct";
            asset.Metadata.AMS.Asset_Class = isPreviewAsset ? "Preview" : "NonPreview";
            asset.Metadata.App_Data = new List<ADIAssetAssetMetadataApp_Data>();

            if (isUltraHdAsset)
            {
                asset.Metadata.App_Data.Add(new ADIAssetAssetMetadataApp_Data
                {
                    Name = "Encoding_Type",
                    Value = "h264-uhd"
                });
            }
            else
            {
                asset.Metadata.App_Data.Add(new ADIAssetAssetMetadataApp_Data
                {
                    Name = "HdContent",
                    Value = isSdAsset ? "n" : "y"
                });
            }

            return asset;
        }

        private static ADIAssetAsset CreatePosterAsset()
        {
            return new ADIAssetAsset()
            {
                Content = new ADIAssetAssetContent
                {
                    Value = "PosterContent"
                },
                Metadata = new ADIAssetAssetMetadata
                {
                    AMS = new ADIAssetAssetMetadataAMS
                    {
                        Asset_ID = "PosterId",
                        Asset_Class = "poster"
                    }
                }
            };
        }
        
        private static ADIAssetMetadata CreateAdiAssetMetadata(string assetId, bool isAdultContent = false)
        {
            return new ADIAssetMetadata
            {
                AMS = new ADIAssetMetadataAMS
                {
                    Asset_ID = assetId
                },
                App_Data = new List<ADIAssetMetadataApp_Data>
                {
                    new ADIAssetMetadataApp_Data
                    {
                        Name = "Audience",
                        Value = isAdultContent ? "Adult" : "Kids"
                    }
                }
            };
        }
    }
}