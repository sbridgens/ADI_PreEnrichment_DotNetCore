using System.Collections.Generic;
using Application.DataAccess.Persistence.Contracts;
using Application.Models;
using Application.Validation.Contracts;
using AutoFixture;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Validation;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.EntityFrameworkCore;
using Xunit;

namespace Infrastructure.Tests.Validation
{
    public class PackageValidatorTests
    {
        private readonly Fixture _fixture = new Fixture();
        private readonly PackageValidator _sut;
        private readonly Mock<IApplicationDbContext> _context = new Mock<IApplicationDbContext>();
        private readonly Mock<IVersionChecker> _versionChecker = new Mock<IVersionChecker>();

        public PackageValidatorTests()
        {
            _sut = new PackageValidator(_versionChecker.Object, _context.Object, NullLogger<PackageValidator>.Instance);
        }
        
        [Fact]
        public void IsValidIngest1()
        {
            var testPackage = new PackageEntry
            {
                IsPackageAnUpdate = false,
                TitlPaIdValue = _fixture.Create<string>()
            };
            _context.Setup(x => x.Adi_Data)
                .ReturnsDbSet(new List<Adi_Data>
                { 
                    _fixture.Build<Adi_Data>()
                    .With(x => x.TitlPaid, testPackage.TitlPaIdValue)
                    .Create() 
                });
            _versionChecker.Setup(x => x.IsHigherVersion(testPackage, It.IsAny<int?>(), It.IsAny<int?>(), false))
                .Returns(true);
            
            _sut.IsValidIngest(testPackage).Should().BeTrue();
            
            _versionChecker.VerifyAll();
        }
        
        [Fact]
        public void IsValidIngest2()
        {
            var testPackage = new PackageEntry
            {
                IsPackageAnUpdate = false,
                TitlPaIdValue = _fixture.Create<string>()
            };
            _context.Setup(x => x.Adi_Data)
                .ReturnsDbSet(new List<Adi_Data>
                { 
                    _fixture.Build<Adi_Data>()
                        .With(x => x.TitlPaid, testPackage.TitlPaIdValue)
                        .Create() 
                });
            _versionChecker.Setup(x => x.IsHigherVersion(testPackage, It.IsAny<int?>(), It.IsAny<int?>(), false))
                .Returns(false);
            
            _sut.IsValidIngest(testPackage).Should().BeFalse();
            
            _versionChecker.VerifyAll();
        }
        
        [Fact]
        public void IsValidIngest3()
        {
            var testPackage = new PackageEntry
            {
                IsPackageAnUpdate = false,
                TitlPaIdValue = _fixture.Create<string>()
            };
            _context.Setup(x => x.Adi_Data)
                .ReturnsDbSet(new List<Adi_Data>());
            
            _sut.IsValidIngest(testPackage).Should().BeTrue();
        }
        
        [Fact]
        public void IsValidIngest4()
        {
            var testPackage = new PackageEntry
            {
                IsPackageAnUpdate = true,
                TitlPaIdValue = _fixture.Create<string>()
            };
            _context.Setup(x => x.Adi_Data)
                .ReturnsDbSet(new List<Adi_Data>
                { 
                    _fixture.Build<Adi_Data>()
                        .With(x => x.TitlPaid, testPackage.TitlPaIdValue)
                        .Create() 
                });
            _versionChecker.Setup(x => x.IsHigherVersion(testPackage, It.IsAny<int?>(), It.IsAny<int?>(), false))
                .Returns(true);
            
            _sut.IsValidIngest(testPackage).Should().BeTrue();
            
            _versionChecker.VerifyAll();
        }
        
        [Fact]
        public void IsValidUpdate1()
        {
            var testPackage = new PackageEntry
            {
                IsPackageAnUpdate = true,
                TitlPaIdValue = _fixture.Create<string>()
            };
            _context.Setup(x => x.Adi_Data)
                .ReturnsDbSet(new List<Adi_Data>
                { 
                    _fixture.Build<Adi_Data>()
                        .With(x => x.TitlPaid, testPackage.TitlPaIdValue)
                        .Create() 
                });
            
            _sut.IsValidUpdate(testPackage).Should().BeTrue();
        }
        
        [Fact]
        public void IsValidUpdate2()
        {
            var testPackage = new PackageEntry
            {
                IsPackageAnUpdate = true,
                TitlPaIdValue = _fixture.Create<string>()
            };
            _context.Setup(x => x.Adi_Data)
                .ReturnsDbSet(new List<Adi_Data>());
            
            //_sut.IsValidUpdate(testPackage).Should().BeFalse();
        }
    }
}