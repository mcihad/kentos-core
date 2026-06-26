using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Kentos.Modules.Settlement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSettlement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "yerlesim");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "iller",
                schema: "yerlesim",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false, comment: "Dahili sayısal birincil anahtar (API'de gösterilmez)")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ad = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, comment: "İl adı"),
                    plaka_kodu = table.Column<int>(type: "integer", nullable: false, comment: "Plaka kodu (1..81)"),
                    uuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()", comment: "Genel UUIDv7 kimlik (API'de 'id' olarak kullanılır)"),
                    surum = table.Column<long>(type: "bigint", nullable: false, comment: "İyimser eşzamanlılık sürüm sayacı"),
                    meta_veri = table.Column<string>(type: "jsonb", nullable: false, comment: "Esnek meta veri (jsonb, camelCase anahtarlar)"),
                    olusturan = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "Kaydı oluşturan kullanıcı"),
                    olusturma_tarihi = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Oluşturma zamanı (UTC)"),
                    guncelleyen = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "Son güncelleyen kullanıcı"),
                    guncelleme_tarihi = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Son güncelleme zamanı (UTC)"),
                    silindi_mi = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Yumuşak silme işareti"),
                    silen = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "Silen kullanıcı"),
                    silme_tarihi = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Silme zamanı (UTC)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_iller", x => x.id);
                },
                comment: "İller");

            migrationBuilder.CreateTable(
                name: "ilceler",
                schema: "yerlesim",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false, comment: "Dahili sayısal birincil anahtar (API'de gösterilmez)")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ad = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, comment: "İlçe adı"),
                    il_id = table.Column<long>(type: "bigint", nullable: false, comment: "İl yabancı anahtarı"),
                    uuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()", comment: "Genel UUIDv7 kimlik (API'de 'id' olarak kullanılır)"),
                    surum = table.Column<long>(type: "bigint", nullable: false, comment: "İyimser eşzamanlılık sürüm sayacı"),
                    meta_veri = table.Column<string>(type: "jsonb", nullable: false, comment: "Esnek meta veri (jsonb, camelCase anahtarlar)"),
                    olusturan = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "Kaydı oluşturan kullanıcı"),
                    olusturma_tarihi = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Oluşturma zamanı (UTC)"),
                    guncelleyen = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "Son güncelleyen kullanıcı"),
                    guncelleme_tarihi = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Son güncelleme zamanı (UTC)"),
                    silindi_mi = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Yumuşak silme işareti"),
                    silen = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "Silen kullanıcı"),
                    silme_tarihi = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Silme zamanı (UTC)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ilceler", x => x.id);
                    table.ForeignKey(
                        name: "fk_ilceler_provinces_province_id",
                        column: x => x.il_id,
                        principalSchema: "yerlesim",
                        principalTable: "iller",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "İlçeler");

            migrationBuilder.CreateTable(
                name: "mahalleler",
                schema: "yerlesim",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false, comment: "Dahili sayısal birincil anahtar (API'de gösterilmez)")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ad = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, comment: "Mahalle adı"),
                    ilce_id = table.Column<long>(type: "bigint", nullable: false, comment: "İlçe yabancı anahtarı"),
                    posta_kodu = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true, comment: "Posta kodu"),
                    merkez = table.Column<Point>(type: "geometry(Point, 4326)", nullable: true, comment: "Merkez nokta (SRID 4326)"),
                    sinir = table.Column<MultiPolygon>(type: "geometry(MultiPolygon, 4326)", nullable: true, comment: "Sınır çokgeni (SRID 4326)"),
                    uuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()", comment: "Genel UUIDv7 kimlik (API'de 'id' olarak kullanılır)"),
                    surum = table.Column<long>(type: "bigint", nullable: false, comment: "İyimser eşzamanlılık sürüm sayacı"),
                    meta_veri = table.Column<string>(type: "jsonb", nullable: false, comment: "Esnek meta veri (jsonb, camelCase anahtarlar)"),
                    olusturan = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "Kaydı oluşturan kullanıcı"),
                    olusturma_tarihi = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Oluşturma zamanı (UTC)"),
                    guncelleyen = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "Son güncelleyen kullanıcı"),
                    guncelleme_tarihi = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Son güncelleme zamanı (UTC)"),
                    silindi_mi = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Yumuşak silme işareti"),
                    silen = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "Silen kullanıcı"),
                    silme_tarihi = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Silme zamanı (UTC)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mahalleler", x => x.id);
                    table.ForeignKey(
                        name: "fk_mahalleler_ilceler_district_id",
                        column: x => x.ilce_id,
                        principalSchema: "yerlesim",
                        principalTable: "ilceler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Mahalleler");

            migrationBuilder.CreateIndex(
                name: "ix_ilceler_il_id_ad",
                schema: "yerlesim",
                table: "ilceler",
                columns: new[] { "il_id", "ad" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_ilceler_uuid",
                schema: "yerlesim",
                table: "ilceler",
                column: "uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_iller_ad",
                schema: "yerlesim",
                table: "iller",
                column: "ad");

            migrationBuilder.CreateIndex(
                name: "ix_iller_plaka_kodu",
                schema: "yerlesim",
                table: "iller",
                column: "plaka_kodu",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_iller_uuid",
                schema: "yerlesim",
                table: "iller",
                column: "uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mahalleler_ilce_id_ad",
                schema: "yerlesim",
                table: "mahalleler",
                columns: new[] { "ilce_id", "ad" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_mahalleler_uuid",
                schema: "yerlesim",
                table: "mahalleler",
                column: "uuid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mahalleler",
                schema: "yerlesim");

            migrationBuilder.DropTable(
                name: "ilceler",
                schema: "yerlesim");

            migrationBuilder.DropTable(
                name: "iller",
                schema: "yerlesim");
        }
    }
}
