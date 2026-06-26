using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Kentos.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialAuditing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "denetim");

            migrationBuilder.CreateTable(
                name: "denetim_kayitlari",
                schema: "denetim",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false, comment: "Birincil anahtar")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    varlik_tipi = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false, comment: "Değişen varlık tipi (CLR adı)"),
                    varlik_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "Kaydın genel kimliği (Uuid)"),
                    tablo_adi = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true, comment: "Veritabanı tablo adı"),
                    modul = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "Modül slug'ı"),
                    islem = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, comment: "İşlem türü (Insert/Update/Delete)"),
                    degisiklikler = table.Column<string>(type: "jsonb", nullable: false, comment: "Alan bazlı değişiklikler [{ field, oldValue, newValue }]"),
                    ip_adresi = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "İstemci IP adresi"),
                    kullanici_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "İşlemi yapan kullanıcı kimliği"),
                    kullanici_adi = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "İşlemi yapan kullanıcı adı"),
                    olusturma_tarihi = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Kayıt zamanı (UTC)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_denetim_kayitlari", x => x.id);
                },
                comment: "Veri katmanı denetim kayıtları");

            migrationBuilder.CreateTable(
                name: "hata_kayitlari",
                schema: "denetim",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false, comment: "Birincil anahtar")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    parmakizi = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "Hata parmak izi (gruplama anahtarı)"),
                    koken = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, comment: "Hata kökeni (Server/Client)"),
                    modul = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "Hatanın oluştuğu modül slug'ı"),
                    mesaj = table.Column<string>(type: "text", nullable: false, comment: "Hata mesajı"),
                    istisna_tipi = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false, comment: "İstisna tipi (CLR adı)"),
                    yigin_izi = table.Column<string>(type: "text", nullable: true, comment: "Yığın izi (stack trace)"),
                    kaynak = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "Hata kaynağı (assembly/metot)"),
                    dosya_adi = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true, comment: "Kaynak dosya adı"),
                    satir_no = table.Column<int>(type: "integer", nullable: true, comment: "Kaynak satır numarası"),
                    http_metot = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true, comment: "HTTP metodu"),
                    yol = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true, comment: "İstek yolu"),
                    sorgu_dizesi = table.Column<string>(type: "text", nullable: true, comment: "İstek sorgu dizesi"),
                    durum_kodu = table.Column<int>(type: "integer", nullable: false, defaultValue: 500, comment: "HTTP durum kodu"),
                    ip_adresi = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "İstemci IP adresi"),
                    istemci_bilgisi = table.Column<string>(type: "text", nullable: true, comment: "İstemci/tarayıcı kimliği (User-Agent)"),
                    basliklar = table.Column<string>(type: "jsonb", nullable: false, comment: "İstek başlıkları (jsonb)"),
                    kullanici_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "Kullanıcı kimliği"),
                    kullanici_adi = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "Kullanıcı adı"),
                    durum = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, comment: "Triyaj durumu (New/Investigating/Resolved/Ignored)"),
                    gelistirici_notu = table.Column<string>(type: "text", nullable: true, comment: "Geliştirici notu"),
                    tekrar_sayisi = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "Aynı hatanın görülme sayısı"),
                    ilk_gorulme = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "İlk görülme zamanı (UTC)"),
                    son_gorulme = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Son görülme zamanı (UTC)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hata_kayitlari", x => x.id);
                },
                comment: "Yakalanan hata kayıtları (parmak izi ile gruplanır)");

            migrationBuilder.CreateIndex(
                name: "ix_denetim_kayitlari_olusturma_tarihi",
                schema: "denetim",
                table: "denetim_kayitlari",
                column: "olusturma_tarihi");

            migrationBuilder.CreateIndex(
                name: "ix_denetim_kayitlari_varlik_tipi_varlik_id",
                schema: "denetim",
                table: "denetim_kayitlari",
                columns: new[] { "varlik_tipi", "varlik_id" });

            migrationBuilder.CreateIndex(
                name: "ix_hata_kayitlari_durum",
                schema: "denetim",
                table: "hata_kayitlari",
                column: "durum");

            migrationBuilder.CreateIndex(
                name: "ix_hata_kayitlari_koken",
                schema: "denetim",
                table: "hata_kayitlari",
                column: "koken");

            migrationBuilder.CreateIndex(
                name: "ix_hata_kayitlari_parmakizi",
                schema: "denetim",
                table: "hata_kayitlari",
                column: "parmakizi",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_hata_kayitlari_son_gorulme",
                schema: "denetim",
                table: "hata_kayitlari",
                column: "son_gorulme");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "denetim_kayitlari",
                schema: "denetim");

            migrationBuilder.DropTable(
                name: "hata_kayitlari",
                schema: "denetim");
        }
    }
}
