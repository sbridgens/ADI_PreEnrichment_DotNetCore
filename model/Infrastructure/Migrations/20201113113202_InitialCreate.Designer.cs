// <auto-generated />
using System;
using Infrastructure.DataAccess.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Infrastructure.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20201113113202_InitialCreate")]
    partial class InitialCreate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                .HasAnnotation("ProductVersion", "3.1.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            modelBuilder.Entity("Domain.Entities.Adi_Data", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("ContentTsFile")
                        .HasColumnName("ContentTSFile")
                        .HasColumnType("text");

                    b.Property<string>("ContentTsFileChecksum")
                        .HasColumnName("ContentTSFileChecksum")
                        .HasColumnType("text");

                    b.Property<string>("ContentTsFilePaid")
                        .HasColumnName("ContentTsFilePaid")
                        .HasColumnType("text");

                    b.Property<string>("ContentTsFileSize")
                        .HasColumnName("ContentTSFileSize")
                        .HasColumnType("text");

                    b.Property<string>("EnrichedAdi")
                        .HasColumnName("EnrichedADI")
                        .HasColumnType("text");

                    b.Property<DateTime?>("Enrichment_DateTime")
                        .HasColumnName("Enrichment_DateTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<Guid>("IngestUUID")
                        .HasColumnType("uuid");

                    b.Property<string>("Licensing_Window_End")
                        .HasColumnName("Licensing_Window_End")
                        .HasColumnType("text");

                    b.Property<string>("OriginalAdi")
                        .HasColumnName("OriginalADI")
                        .HasColumnType("text");

                    b.Property<string>("PreviewFile")
                        .HasColumnName("PreviewFile")
                        .HasColumnType("text");

                    b.Property<string>("PreviewFileChecksum")
                        .HasColumnName("PreviewFileChecksum")
                        .HasColumnType("text");

                    b.Property<string>("PreviewFilePaid")
                        .HasColumnName("PreviewFilePaid")
                        .HasColumnType("text");

                    b.Property<string>("PreviewFileSize")
                        .HasColumnName("PreviewFileSize")
                        .HasColumnType("text");

                    b.Property<DateTime?>("ProcessedDateTime")
                        .HasColumnName("ProcessedDateTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("ProviderId")
                        .HasColumnName("ProviderId")
                        .HasColumnType("text");

                    b.Property<string>("TitlPaid")
                        .HasColumnName("TITLPAID")
                        .HasColumnType("text");

                    b.Property<string>("TmsId")
                        .HasColumnName("TMSID")
                        .HasColumnType("text");

                    b.Property<string>("UpdateAdi")
                        .HasColumnName("UpdateAdi")
                        .HasColumnType("text");

                    b.Property<DateTime?>("Update_DateTime")
                        .HasColumnName("Update_DateTime")
                        .HasColumnType("timestamp without time zone");

                    b.Property<int?>("VersionMajor")
                        .HasColumnName("VersionMajor")
                        .HasColumnType("integer");

                    b.Property<int?>("VersionMinor")
                        .HasColumnName("VersionMinor")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.HasIndex("IngestUUID");

                    b.ToTable("Adi_Data","adi");
                });

            modelBuilder.Entity("Domain.Entities.CategoryMapping", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("CategoryValue")
                        .HasColumnName("CategoryValue")
                        .HasColumnType("text");

                    b.Property<string>("ProviderId")
                        .HasColumnName("ProviderId")
                        .HasColumnType("text");

                    b.Property<string>("ProviderName")
                        .HasColumnName("ProviderName")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("CategoryMapping","adi");
                });

            modelBuilder.Entity("Domain.Entities.GN_Api_Lookup", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("GN_TMSID")
                        .HasColumnName("GN_TMSID")
                        .HasColumnType("text");

                    b.Property<string>("GN_connectorId")
                        .HasColumnName("GN_connectorId")
                        .HasColumnType("text");

                    b.Property<string>("GnLayer1Data")
                        .HasColumnName("GnLayer1Data")
                        .HasColumnType("text");

                    b.Property<string>("GnLayer2Data")
                        .HasColumnName("GnLayer2Data")
                        .HasColumnType("text");

                    b.Property<string>("GnMapData")
                        .HasColumnName("GnMapData")
                        .HasColumnType("text");

                    b.Property<Guid>("IngestUUID")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("IngestUUID");

                    b.ToTable("GN_Api_Lookup","adi");
                });

            modelBuilder.Entity("Domain.Entities.GN_ImageLookup", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<int>("Image_AdiOrder")
                        .HasColumnName("Image_AdiOrder")
                        .HasColumnType("integer");

                    b.Property<string>("Image_Lookup")
                        .HasColumnName("Image_Lookup")
                        .HasColumnType("text");

                    b.Property<string>("Image_Mapping")
                        .HasColumnName("Image_Mapping")
                        .HasColumnType("text");

                    b.Property<string>("Mapping_Config")
                        .HasColumnName("Mapping_Config")
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("GN_ImageLookup","adi");
                });

            modelBuilder.Entity("Domain.Entities.GN_Mapping_Data", b =>
                {
                    b.Property<Guid>("IngestUUID")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<DateTime?>("GN_Availability_End")
                        .HasColumnName("GN_Availability_End")
                        .HasColumnType("timestamp without time zone");

                    b.Property<DateTime?>("GN_Availability_Start")
                        .HasColumnName("GN_Availability_Start")
                        .HasColumnType("timestamp without time zone");

                    b.Property<long?>("GN_EpisodeNumber")
                        .HasColumnName("GN_EpisodeNumber")
                        .HasColumnType("bigint");

                    b.Property<string>("GN_EpisodeTitle")
                        .HasColumnName("GN_EpisodeTitle")
                        .HasColumnType("text");

                    b.Property<string>("GN_Images")
                        .HasColumnName("GN_Images")
                        .HasColumnType("text");

                    b.Property<string>("GN_Paid")
                        .HasColumnName("GN_Paid")
                        .HasColumnType("text");

                    b.Property<string>("GN_Pid")
                        .HasColumnName("GN_Pid")
                        .HasColumnType("text");

                    b.Property<string>("GN_ProviderId")
                        .HasColumnName("GN_ProviderId")
                        .HasColumnType("text");

                    b.Property<string>("GN_RootID")
                        .HasColumnName("GN_RootID")
                        .HasColumnType("text");

                    b.Property<int?>("GN_SeasonId")
                        .HasColumnName("GN_SeasonId")
                        .HasColumnType("integer");

                    b.Property<int?>("GN_SeasonNumber")
                        .HasColumnName("GN_SeasonNumber")
                        .HasColumnType("integer");

                    b.Property<long?>("GN_SeriesId")
                        .HasColumnName("GN_SeriesId")
                        .HasColumnType("bigint");

                    b.Property<string>("GN_SeriesTitle")
                        .HasColumnName("GN_SeriesTitle")
                        .HasColumnType("text");

                    b.Property<string>("GN_Status")
                        .HasColumnName("GN_Status")
                        .HasColumnType("text");

                    b.Property<string>("GN_TMSID")
                        .HasColumnName("GN_TMSID")
                        .HasColumnType("text");

                    b.Property<string>("GN_connectorId")
                        .HasColumnName("GN_connectorId")
                        .HasColumnType("text");

                    b.Property<DateTime?>("GN_creationDate")
                        .HasColumnName("GN_creationDate")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("GN_programMappingId")
                        .HasColumnName("GN_programMappingId")
                        .HasColumnType("text");

                    b.Property<string>("GN_updateId")
                        .HasColumnName("GN_updateId")
                        .HasColumnType("text");

                    b.Property<int>("Id")
                        .HasColumnName("id")
                        .HasColumnType("integer");

                    b.HasKey("IngestUUID");

                    b.ToTable("GN_Mapping_Data","adi");
                });

            modelBuilder.Entity("Domain.Entities.GnProgramTypeLookup", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("GnProgramSubType")
                        .HasColumnName("GnProgramSubType")
                        .HasColumnType("text");

                    b.Property<string>("GnProgramType")
                        .HasColumnName("GnProgramType")
                        .HasColumnType("text");

                    b.Property<string>("LgiProgramType")
                        .HasColumnName("LgiProgramType")
                        .HasColumnType("text");

                    b.Property<int>("LgiProgramTypeId")
                        .HasColumnName("LgiProgramTypeId")
                        .HasColumnType("integer");

                    b.HasKey("Id");

                    b.ToTable("GnProgramTypeLookup","adi");
                });

            modelBuilder.Entity("Domain.Entities.LatestUpdateIds", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<bool>("InOperation")
                        .HasColumnName("InOperation")
                        .HasColumnType("boolean");

                    b.Property<long>("LastLayer1UpdateIdChecked")
                        .HasColumnName("LastLayer1UpdateIdChecked")
                        .HasColumnType("bigint");

                    b.Property<long>("LastLayer2UpdateIdChecked")
                        .HasColumnName("LastLayer2UpdateIdChecked")
                        .HasColumnType("bigint");

                    b.Property<long>("LastMappingUpdateIdChecked")
                        .HasColumnName("LastMappingUpdateIdChecked")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.ToTable("LatestUpdateIds","adi");
                });

            modelBuilder.Entity("Domain.Entities.Layer1UpdateTracking", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("GN_Paid")
                        .HasColumnName("GN_Paid")
                        .HasColumnType("text");

                    b.Property<string>("GN_TMSID")
                        .HasColumnName("GN_TMSID")
                        .HasColumnType("text");

                    b.Property<Guid>("IngestUUID")
                        .HasColumnType("uuid");

                    b.Property<string>("Layer1_MaxUpdateId")
                        .HasColumnName("Layer1_MaxUpdateId")
                        .HasColumnType("text");

                    b.Property<string>("Layer1_NextUpdateId")
                        .HasColumnName("Layer1_NextUpdateId")
                        .HasColumnType("text");

                    b.Property<string>("Layer1_RootId")
                        .HasColumnName("Layer1_RootId")
                        .HasColumnType("text");

                    b.Property<DateTime>("Layer1_UpdateDate")
                        .HasColumnName("Layer1_UpdateDate")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("Layer1_UpdateId")
                        .HasColumnName("Layer1_UpdateId")
                        .HasColumnType("text");

                    b.Property<bool>("RequiresEnrichment")
                        .HasColumnName("RequiresEnrichment")
                        .HasColumnType("boolean");

                    b.Property<DateTime>("UpdatesChecked")
                        .HasColumnName("UpdatesChecked")
                        .HasColumnType("timestamp without time zone");

                    b.HasKey("Id");

                    b.HasIndex("IngestUUID");

                    b.ToTable("Layer1UpdateTracking","adi");
                });

            modelBuilder.Entity("Domain.Entities.Layer2UpdateTracking", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("GN_Paid")
                        .HasColumnName("GN_Paid")
                        .HasColumnType("text");

                    b.Property<string>("GN_connectorId")
                        .HasColumnName("GN_connectorId")
                        .HasColumnType("text");

                    b.Property<Guid>("IngestUUID")
                        .HasColumnType("uuid");

                    b.Property<string>("Layer2_MaxUpdateId")
                        .HasColumnName("Layer2_MaxUpdateId")
                        .HasColumnType("text");

                    b.Property<string>("Layer2_NextUpdateId")
                        .HasColumnName("Layer2_NextUpdateId")
                        .HasColumnType("text");

                    b.Property<string>("Layer2_RootId")
                        .HasColumnName("Layer2_RootId")
                        .HasColumnType("text");

                    b.Property<DateTime>("Layer2_UpdateDate")
                        .HasColumnName("Layer2_UpdateDate")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("Layer2_UpdateId")
                        .HasColumnName("Layer2_UpdateId")
                        .HasColumnType("text");

                    b.Property<bool>("RequiresEnrichment")
                        .HasColumnName("RequiresEnrichment")
                        .HasColumnType("boolean");

                    b.Property<DateTime>("UpdatesChecked")
                        .HasColumnName("UpdatesChecked")
                        .HasColumnType("timestamp without time zone");

                    b.HasKey("Id");

                    b.HasIndex("IngestUUID");

                    b.ToTable("Layer2UpdateTracking","adi");
                });

            modelBuilder.Entity("Domain.Entities.MappingsUpdateTracking", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("GN_ProviderId")
                        .HasColumnName("GN_ProviderId")
                        .HasColumnType("text");

                    b.Property<Guid>("IngestUUID")
                        .HasColumnType("uuid");

                    b.Property<string>("Mapping_MaxUpdateId")
                        .HasColumnName("Mapping_MaxUpdateId")
                        .HasColumnType("text");

                    b.Property<string>("Mapping_NextUpdateId")
                        .HasColumnName("Mapping_NextUpdateId")
                        .HasColumnType("text");

                    b.Property<string>("Mapping_RootId")
                        .HasColumnName("Mapping_RootId")
                        .HasColumnType("text");

                    b.Property<DateTime>("Mapping_UpdateDate")
                        .HasColumnName("Mapping_UpdateDate")
                        .HasColumnType("timestamp without time zone");

                    b.Property<string>("Mapping_UpdateId")
                        .HasColumnName("Mapping_UpdateId")
                        .HasColumnType("text");

                    b.Property<bool>("RequiresEnrichment")
                        .HasColumnName("RequiresEnrichment")
                        .HasColumnType("boolean");

                    b.Property<DateTime>("UpdatesChecked")
                        .HasColumnName("UpdatesChecked")
                        .HasColumnType("timestamp without time zone");

                    b.HasKey("Id");

                    b.HasIndex("IngestUUID");

                    b.ToTable("MappingsUpdateTracking","adi");
                });

            modelBuilder.Entity("Domain.Entities.Adi_Data", b =>
                {
                    b.HasOne("Domain.Entities.GN_Mapping_Data", "GnMappingData")
                        .WithMany()
                        .HasForeignKey("IngestUUID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Domain.Entities.GN_Api_Lookup", b =>
                {
                    b.HasOne("Domain.Entities.GN_Mapping_Data", "GnMappingData")
                        .WithMany()
                        .HasForeignKey("IngestUUID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Domain.Entities.Layer1UpdateTracking", b =>
                {
                    b.HasOne("Domain.Entities.GN_Mapping_Data", "GnMappingData")
                        .WithMany()
                        .HasForeignKey("IngestUUID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Domain.Entities.Layer2UpdateTracking", b =>
                {
                    b.HasOne("Domain.Entities.GN_Mapping_Data", "GnMappingData")
                        .WithMany()
                        .HasForeignKey("IngestUUID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Domain.Entities.MappingsUpdateTracking", b =>
                {
                    b.HasOne("Domain.Entities.GN_Mapping_Data", "GnMappingData")
                        .WithMany()
                        .HasForeignKey("IngestUUID")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
