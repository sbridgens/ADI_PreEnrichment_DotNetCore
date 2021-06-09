using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace Infrastructure.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "adi");

            migrationBuilder.CreateTable(
                name: "CategoryMapping",
                schema: "adi",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProviderId = table.Column<string>(nullable: true),
                    ProviderName = table.Column<string>(nullable: true),
                    CategoryValue = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoryMapping", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GN_ImageLookup",
                schema: "adi",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Image_Lookup = table.Column<string>(nullable: true),
                    Image_Mapping = table.Column<string>(nullable: true),
                    Image_AdiOrder = table.Column<int>(nullable: false),
                    Mapping_Config = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GN_ImageLookup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GN_Mapping_Data",
                schema: "adi",
                columns: table => new
                {
                    IngestUUID = table.Column<Guid>(nullable: false),
                    id = table.Column<int>(nullable: false),
                    GN_TMSID = table.Column<string>(nullable: true),
                    GN_RootID = table.Column<string>(nullable: true),
                    GN_Status = table.Column<string>(nullable: true),
                    GN_ProviderId = table.Column<string>(nullable: true),
                    GN_Paid = table.Column<string>(nullable: true),
                    GN_Pid = table.Column<string>(nullable: true),
                    GN_programMappingId = table.Column<string>(nullable: true),
                    GN_creationDate = table.Column<DateTime>(nullable: true),
                    GN_updateId = table.Column<string>(nullable: true),
                    GN_Images = table.Column<string>(nullable: true),
                    GN_Availability_Start = table.Column<DateTime>(nullable: true),
                    GN_Availability_End = table.Column<DateTime>(nullable: true),
                    GN_connectorId = table.Column<string>(nullable: true),
                    GN_SeasonId = table.Column<int>(nullable: true),
                    GN_SeasonNumber = table.Column<int>(nullable: true),
                    GN_SeriesId = table.Column<long>(nullable: true),
                    GN_EpisodeNumber = table.Column<long>(nullable: true),
                    GN_SeriesTitle = table.Column<string>(nullable: true),
                    GN_EpisodeTitle = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GN_Mapping_Data", x => x.IngestUUID);
                });

            migrationBuilder.CreateTable(
                name: "GnProgramTypeLookup",
                schema: "adi",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GnProgramType = table.Column<string>(nullable: true),
                    GnProgramSubType = table.Column<string>(nullable: true),
                    LgiProgramType = table.Column<string>(nullable: true),
                    LgiProgramTypeId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GnProgramTypeLookup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LatestUpdateIds",
                schema: "adi",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LastMappingUpdateIdChecked = table.Column<long>(nullable: false),
                    LastLayer1UpdateIdChecked = table.Column<long>(nullable: false),
                    LastLayer2UpdateIdChecked = table.Column<long>(nullable: false),
                    InOperation = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LatestUpdateIds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Adi_Data",
                schema: "adi",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IngestUUID = table.Column<Guid>(nullable: false),
                    TITLPAID = table.Column<string>(nullable: true),
                    OriginalADI = table.Column<string>(nullable: true),
                    VersionMajor = table.Column<int>(nullable: true),
                    VersionMinor = table.Column<int>(nullable: true),
                    ProviderId = table.Column<string>(nullable: true),
                    TMSID = table.Column<string>(nullable: true),
                    ProcessedDateTime = table.Column<DateTime>(nullable: true),
                    ContentTSFile = table.Column<string>(nullable: true),
                    ContentTsFilePaid = table.Column<string>(nullable: true),
                    ContentTSFileChecksum = table.Column<string>(nullable: true),
                    ContentTSFileSize = table.Column<string>(nullable: true),
                    PreviewFile = table.Column<string>(nullable: true),
                    PreviewFilePaid = table.Column<string>(nullable: true),
                    PreviewFileChecksum = table.Column<string>(nullable: true),
                    PreviewFileSize = table.Column<string>(nullable: true),
                    EnrichedADI = table.Column<string>(nullable: true),
                    Enrichment_DateTime = table.Column<DateTime>(nullable: true),
                    UpdateAdi = table.Column<string>(nullable: true),
                    Update_DateTime = table.Column<DateTime>(nullable: true),
                    Licensing_Window_End = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Adi_Data", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Adi_Data_GN_Mapping_Data_IngestUUID",
                        column: x => x.IngestUUID,
                        principalSchema: "adi",
                        principalTable: "GN_Mapping_Data",
                        principalColumn: "IngestUUID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GN_Api_Lookup",
                schema: "adi",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IngestUUID = table.Column<Guid>(nullable: false),
                    GN_TMSID = table.Column<string>(nullable: true),
                    GN_connectorId = table.Column<string>(nullable: true),
                    GnMapData = table.Column<string>(nullable: true),
                    GnLayer1Data = table.Column<string>(nullable: true),
                    GnLayer2Data = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GN_Api_Lookup", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GN_Api_Lookup_GN_Mapping_Data_IngestUUID",
                        column: x => x.IngestUUID,
                        principalSchema: "adi",
                        principalTable: "GN_Mapping_Data",
                        principalColumn: "IngestUUID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Layer1UpdateTracking",
                schema: "adi",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IngestUUID = table.Column<Guid>(nullable: false),
                    GN_Paid = table.Column<string>(nullable: true),
                    GN_TMSID = table.Column<string>(nullable: true),
                    Layer1_UpdateId = table.Column<string>(nullable: true),
                    Layer1_UpdateDate = table.Column<DateTime>(nullable: false),
                    Layer1_NextUpdateId = table.Column<string>(nullable: true),
                    Layer1_MaxUpdateId = table.Column<string>(nullable: true),
                    Layer1_RootId = table.Column<string>(nullable: true),
                    UpdatesChecked = table.Column<DateTime>(nullable: false),
                    RequiresEnrichment = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Layer1UpdateTracking", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Layer1UpdateTracking_GN_Mapping_Data_IngestUUID",
                        column: x => x.IngestUUID,
                        principalSchema: "adi",
                        principalTable: "GN_Mapping_Data",
                        principalColumn: "IngestUUID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Layer2UpdateTracking",
                schema: "adi",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IngestUUID = table.Column<Guid>(nullable: false),
                    GN_Paid = table.Column<string>(nullable: true),
                    GN_connectorId = table.Column<string>(nullable: true),
                    Layer2_UpdateId = table.Column<string>(nullable: true),
                    Layer2_UpdateDate = table.Column<DateTime>(nullable: false),
                    Layer2_NextUpdateId = table.Column<string>(nullable: true),
                    Layer2_MaxUpdateId = table.Column<string>(nullable: true),
                    Layer2_RootId = table.Column<string>(nullable: true),
                    UpdatesChecked = table.Column<DateTime>(nullable: false),
                    RequiresEnrichment = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Layer2UpdateTracking", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Layer2UpdateTracking_GN_Mapping_Data_IngestUUID",
                        column: x => x.IngestUUID,
                        principalSchema: "adi",
                        principalTable: "GN_Mapping_Data",
                        principalColumn: "IngestUUID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MappingsUpdateTracking",
                schema: "adi",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    IngestUUID = table.Column<Guid>(nullable: false),
                    GN_ProviderId = table.Column<string>(nullable: true),
                    Mapping_UpdateId = table.Column<string>(nullable: true),
                    Mapping_UpdateDate = table.Column<DateTime>(nullable: false),
                    Mapping_NextUpdateId = table.Column<string>(nullable: true),
                    Mapping_MaxUpdateId = table.Column<string>(nullable: true),
                    Mapping_RootId = table.Column<string>(nullable: true),
                    UpdatesChecked = table.Column<DateTime>(nullable: false),
                    RequiresEnrichment = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MappingsUpdateTracking", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MappingsUpdateTracking_GN_Mapping_Data_IngestUUID",
                        column: x => x.IngestUUID,
                        principalSchema: "adi",
                        principalTable: "GN_Mapping_Data",
                        principalColumn: "IngestUUID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Adi_Data_IngestUUID",
                schema: "adi",
                table: "Adi_Data",
                column: "IngestUUID");

            migrationBuilder.CreateIndex(
                name: "IX_GN_Api_Lookup_IngestUUID",
                schema: "adi",
                table: "GN_Api_Lookup",
                column: "IngestUUID");

            migrationBuilder.CreateIndex(
                name: "IX_Layer1UpdateTracking_IngestUUID",
                schema: "adi",
                table: "Layer1UpdateTracking",
                column: "IngestUUID");

            migrationBuilder.CreateIndex(
                name: "IX_Layer2UpdateTracking_IngestUUID",
                schema: "adi",
                table: "Layer2UpdateTracking",
                column: "IngestUUID");

            migrationBuilder.CreateIndex(
                name: "IX_MappingsUpdateTracking_IngestUUID",
                schema: "adi",
                table: "MappingsUpdateTracking",
                column: "IngestUUID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Adi_Data",
                schema: "adi");

            migrationBuilder.DropTable(
                name: "CategoryMapping",
                schema: "adi");

            migrationBuilder.DropTable(
                name: "GN_Api_Lookup",
                schema: "adi");

            migrationBuilder.DropTable(
                name: "GN_ImageLookup",
                schema: "adi");

            migrationBuilder.DropTable(
                name: "GnProgramTypeLookup",
                schema: "adi");

            migrationBuilder.DropTable(
                name: "LatestUpdateIds",
                schema: "adi");

            migrationBuilder.DropTable(
                name: "Layer1UpdateTracking",
                schema: "adi");

            migrationBuilder.DropTable(
                name: "Layer2UpdateTracking",
                schema: "adi");

            migrationBuilder.DropTable(
                name: "MappingsUpdateTracking",
                schema: "adi");

            migrationBuilder.DropTable(
                name: "GN_Mapping_Data",
                schema: "adi");
        }
    }
}
