using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Kentos.Modules.Hesap.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialHesap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "hesap");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "departmanlar",
                schema: "hesap",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false, comment: "Dahili sayısal birincil anahtar (API'de gösterilmez)")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ad = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false, comment: "Departman adı"),
                    ust_departman_id = table.Column<long>(type: "bigint", nullable: true, comment: "Üst departman kimliği (kök için boş)"),
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
                    table.PrimaryKey("pk_departmanlar", x => x.id);
                    table.ForeignKey(
                        name: "fk_departmanlar_departmanlar_parent_id",
                        column: x => x.ust_departman_id,
                        principalSchema: "hesap",
                        principalTable: "departmanlar",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Departmanlar (ağaç yapısı)");

            migrationBuilder.CreateTable(
                name: "erisim_politikalari",
                schema: "hesap",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false, comment: "Dahili sayısal birincil anahtar (API'de gösterilmez)")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    konu_tipi = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, comment: "Politikanın hedefi: User (kullanıcı) veya Group (grup)"),
                    konu_id = table.Column<long>(type: "bigint", nullable: false, comment: "Hedef kullanıcı veya grubun dahili kimliği"),
                    tur = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, comment: "Politika türü: Time (zaman) veya Ip (IP)"),
                    etki = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, comment: "Etki: Allow (izin) veya Deny (engelle)"),
                    deger = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, comment: "CIDR (IP) veya 'SS:dd-SS:dd' (zaman) değeri"),
                    oncelik = table.Column<int>(type: "integer", nullable: false, comment: "Değerlendirme önceliği (küçük önce)"),
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
                    table.PrimaryKey("pk_erisim_politikalari", x => x.id);
                },
                comment: "Erişim politikaları (giriş anında IP/zaman kontrolü)");

            migrationBuilder.CreateTable(
                name: "kullanici_gruplari",
                schema: "hesap",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false, comment: "Dahili sayısal birincil anahtar (API'de gösterilmez)")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ad = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false, comment: "Grup adı"),
                    aciklama = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "Grup açıklaması"),
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
                    table.PrimaryKey("pk_kullanici_gruplari", x => x.id);
                },
                comment: "Kullanıcı grupları");

            migrationBuilder.CreateTable(
                name: "kullanicilar",
                schema: "hesap",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false, comment: "Dahili sayısal birincil anahtar (API'de gösterilmez)")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    uuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()", comment: "Genel UUIDv7 kimlik (API'de 'id' olarak kullanılır)"),
                    ad_soyad = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "Ad soyad"),
                    olusturan = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "Kaydı oluşturan kullanıcı"),
                    olusturma_tarihi = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Oluşturma zamanı (UTC)"),
                    guncelleyen = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "Son güncelleyen kullanıcı"),
                    guncelleme_tarihi = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Son güncelleme zamanı (UTC)"),
                    silindi_mi = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Yumuşak silme işareti"),
                    silen = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "Silen kullanıcı"),
                    silme_tarihi = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Silme zamanı (UTC)"),
                    kullanici_adi = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "Kullanıcı adı"),
                    normal_kullanici_adi = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "Normalize edilmiş kullanıcı adı"),
                    e_posta = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "E-posta adresi"),
                    normal_e_posta = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "Normalize edilmiş e-posta"),
                    e_posta_dogrulandi = table.Column<bool>(type: "boolean", nullable: false, comment: "E-posta doğrulandı mı"),
                    parola_hash = table.Column<string>(type: "text", nullable: true, comment: "Parola özeti"),
                    guvenlik_damgasi = table.Column<string>(type: "text", nullable: true, comment: "Güvenlik damgası"),
                    eszamanlilik_damgasi = table.Column<string>(type: "text", nullable: true, comment: "Eşzamanlılık damgası"),
                    telefon = table.Column<string>(type: "text", nullable: true, comment: "Telefon numarası"),
                    telefon_dogrulandi = table.Column<bool>(type: "boolean", nullable: false, comment: "Telefon doğrulandı mı"),
                    iki_faktor_etkin = table.Column<bool>(type: "boolean", nullable: false, comment: "İki faktörlü doğrulama etkin mi"),
                    kilit_bitis = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Kilit bitiş zamanı"),
                    kilit_etkin = table.Column<bool>(type: "boolean", nullable: false, comment: "Kilitlenebilir mi"),
                    basarisiz_giris_sayisi = table.Column<int>(type: "integer", nullable: false, comment: "Ardışık başarısız giriş sayısı")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_kullanicilar", x => x.id);
                },
                comment: "Kullanıcılar");

            migrationBuilder.CreateTable(
                name: "roller",
                schema: "hesap",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false, comment: "Dahili sayısal birincil anahtar (API'de gösterilmez)")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    uuid = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()", comment: "Genel UUIDv7 kimlik (API'de 'id' olarak kullanılır)"),
                    aciklama = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "Rol açıklaması"),
                    olusturan = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "Kaydı oluşturan kullanıcı"),
                    olusturma_tarihi = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Oluşturma zamanı (UTC)"),
                    guncelleyen = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "Son güncelleyen kullanıcı"),
                    guncelleme_tarihi = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Son güncelleme zamanı (UTC)"),
                    silindi_mi = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Yumuşak silme işareti"),
                    silen = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "Silen kullanıcı"),
                    silme_tarihi = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true, comment: "Silme zamanı (UTC)"),
                    ad = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "Rol adı"),
                    normal_ad = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "Normalize edilmiş rol adı"),
                    eszamanlilik_damgasi = table.Column<string>(type: "text", nullable: true, comment: "Eşzamanlılık damgası")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roller", x => x.id);
                },
                comment: "Roller");

            migrationBuilder.CreateTable(
                name: "yetkiler",
                schema: "hesap",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false, comment: "Dahili sayısal birincil anahtar (API'de gösterilmez)")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    anahtar = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false, comment: "Yetki anahtarı (modul.kaynak.eylem)"),
                    modul = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, comment: "Modül slug"),
                    kaynak = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, comment: "Kaynak adı"),
                    eylem = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "Eylem adı"),
                    baslik = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false, comment: "Görünen başlık"),
                    aciklama = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "Açıklama"),
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
                    table.PrimaryKey("pk_yetkiler", x => x.id);
                },
                comment: "Yetkiler (sistem tarafından otomatik tanımlanır)");

            migrationBuilder.CreateTable(
                name: "kullanici_departmanlari",
                schema: "hesap",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false, comment: "Dahili sayısal birincil anahtar (API'de gösterilmez)")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    kullanici_id = table.Column<long>(type: "bigint", nullable: false, comment: "Kullanıcı kimliği"),
                    departman_id = table.Column<long>(type: "bigint", nullable: false, comment: "Departman kimliği"),
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
                    table.PrimaryKey("pk_kullanici_departmanlari", x => x.id);
                    table.ForeignKey(
                        name: "fk_kullanici_departmanlari_departmanlar_department_id",
                        column: x => x.departman_id,
                        principalSchema: "hesap",
                        principalTable: "departmanlar",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_kullanici_departmanlari_kullanicilar_user_id",
                        column: x => x.kullanici_id,
                        principalSchema: "hesap",
                        principalTable: "kullanicilar",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Kullanıcı departman üyelikleri");

            migrationBuilder.CreateTable(
                name: "kullanici_girisleri",
                schema: "hesap",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    provider_key = table.Column<string>(type: "text", nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_kullanici_girisleri", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "fk_kullanici_girisleri_kullanicilar_user_id",
                        column: x => x.user_id,
                        principalSchema: "hesap",
                        principalTable: "kullanicilar",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Harici kullanıcı girişleri");

            migrationBuilder.CreateTable(
                name: "kullanici_grup_uyeleri",
                schema: "hesap",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false, comment: "Dahili sayısal birincil anahtar (API'de gösterilmez)")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    grup_id = table.Column<long>(type: "bigint", nullable: false, comment: "Grup kimliği"),
                    kullanici_id = table.Column<long>(type: "bigint", nullable: false, comment: "Kullanıcı kimliği"),
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
                    table.PrimaryKey("pk_kullanici_grup_uyeleri", x => x.id);
                    table.ForeignKey(
                        name: "fk_kullanici_grup_uyeleri_kullanici_gruplari_group_id",
                        column: x => x.grup_id,
                        principalSchema: "hesap",
                        principalTable: "kullanici_gruplari",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_kullanici_grup_uyeleri_kullanicilar_user_id",
                        column: x => x.kullanici_id,
                        principalSchema: "hesap",
                        principalTable: "kullanicilar",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Kullanıcı grup üyelikleri");

            migrationBuilder.CreateTable(
                name: "kullanici_iddialari",
                schema: "hesap",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_kullanici_iddialari", x => x.id);
                    table.ForeignKey(
                        name: "fk_kullanici_iddialari_kullanicilar_user_id",
                        column: x => x.user_id,
                        principalSchema: "hesap",
                        principalTable: "kullanicilar",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Kullanıcı iddiaları (claims)");

            migrationBuilder.CreateTable(
                name: "kullanici_tokenlari",
                schema: "hesap",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_kullanici_tokenlari", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "fk_kullanici_tokenlari_kullanicilar_user_id",
                        column: x => x.user_id,
                        principalSchema: "hesap",
                        principalTable: "kullanicilar",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Kullanıcı tokenları");

            migrationBuilder.CreateTable(
                name: "yenileme_tokenlari",
                schema: "hesap",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false, comment: "Dahili sayısal birincil anahtar (API'de gösterilmez)")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    kullanici_id = table.Column<long>(type: "bigint", nullable: false, comment: "Kullanıcı kimliği"),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false, comment: "Token SHA-256 özeti (base64)"),
                    son_kullanma = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, comment: "Son kullanma zamanı (UTC)"),
                    iptal_edildi = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "İptal edildi mi"),
                    yerine_gecen_id = table.Column<long>(type: "bigint", nullable: true, comment: "Yerine geçen token kimliği (rotasyon)"),
                    ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "Talep eden istemci IP'si"),
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
                    table.PrimaryKey("pk_yenileme_tokenlari", x => x.id);
                    table.ForeignKey(
                        name: "fk_yenileme_tokenlari_kullanicilar_user_id",
                        column: x => x.kullanici_id,
                        principalSchema: "hesap",
                        principalTable: "kullanicilar",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Yenileme tokenları (refresh)");

            migrationBuilder.CreateTable(
                name: "kullanici_rolleri",
                schema: "hesap",
                columns: table => new
                {
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    role_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_kullanici_rolleri", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_kullanici_rolleri_kullanicilar_user_id",
                        column: x => x.user_id,
                        principalSchema: "hesap",
                        principalTable: "kullanicilar",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_kullanici_rolleri_roller_role_id",
                        column: x => x.role_id,
                        principalSchema: "hesap",
                        principalTable: "roller",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Kullanıcı rolleri");

            migrationBuilder.CreateTable(
                name: "rol_iddialari",
                schema: "hesap",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rol_iddialari", x => x.id);
                    table.ForeignKey(
                        name: "fk_rol_iddialari_roller_role_id",
                        column: x => x.role_id,
                        principalSchema: "hesap",
                        principalTable: "roller",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Rol iddiaları (claims)");

            migrationBuilder.CreateTable(
                name: "rol_yetkileri",
                schema: "hesap",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false, comment: "Dahili sayısal birincil anahtar (API'de gösterilmez)")
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    rol_id = table.Column<long>(type: "bigint", nullable: false, comment: "Rol kimliği"),
                    yetki_id = table.Column<long>(type: "bigint", nullable: false, comment: "Yetki kimliği"),
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
                    table.PrimaryKey("pk_rol_yetkileri", x => x.id);
                    table.ForeignKey(
                        name: "fk_rol_yetkileri_roller_role_id",
                        column: x => x.rol_id,
                        principalSchema: "hesap",
                        principalTable: "roller",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_rol_yetkileri_yetkiler_permission_id",
                        column: x => x.yetki_id,
                        principalSchema: "hesap",
                        principalTable: "yetkiler",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Rol yetkileri (rol ↔ yetki ataması)");

            migrationBuilder.CreateIndex(
                name: "ix_departmanlar_ad",
                schema: "hesap",
                table: "departmanlar",
                column: "ad");

            migrationBuilder.CreateIndex(
                name: "ix_departmanlar_parent_id",
                schema: "hesap",
                table: "departmanlar",
                column: "ust_departman_id");

            migrationBuilder.CreateIndex(
                name: "ix_departmanlar_uuid",
                schema: "hesap",
                table: "departmanlar",
                column: "uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_erisim_politikalari_konu_tipi_konu_id",
                schema: "hesap",
                table: "erisim_politikalari",
                columns: new[] { "konu_tipi", "konu_id" });

            migrationBuilder.CreateIndex(
                name: "ix_erisim_politikalari_uuid",
                schema: "hesap",
                table: "erisim_politikalari",
                column: "uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_kullanici_departmanlari_department_id",
                schema: "hesap",
                table: "kullanici_departmanlari",
                column: "departman_id");

            migrationBuilder.CreateIndex(
                name: "ix_kullanici_departmanlari_kullanici_id_departman_id",
                schema: "hesap",
                table: "kullanici_departmanlari",
                columns: new[] { "kullanici_id", "departman_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_kullanici_departmanlari_uuid",
                schema: "hesap",
                table: "kullanici_departmanlari",
                column: "uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_kullanici_girisleri_user_id",
                schema: "hesap",
                table: "kullanici_girisleri",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_kullanici_grup_uyeleri_grup_id_kullanici_id",
                schema: "hesap",
                table: "kullanici_grup_uyeleri",
                columns: new[] { "grup_id", "kullanici_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_kullanici_grup_uyeleri_user_id",
                schema: "hesap",
                table: "kullanici_grup_uyeleri",
                column: "kullanici_id");

            migrationBuilder.CreateIndex(
                name: "ix_kullanici_grup_uyeleri_uuid",
                schema: "hesap",
                table: "kullanici_grup_uyeleri",
                column: "uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_kullanici_gruplari_ad",
                schema: "hesap",
                table: "kullanici_gruplari",
                column: "ad");

            migrationBuilder.CreateIndex(
                name: "ix_kullanici_gruplari_uuid",
                schema: "hesap",
                table: "kullanici_gruplari",
                column: "uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_kullanici_iddialari_user_id",
                schema: "hesap",
                table: "kullanici_iddialari",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_kullanici_rolleri_role_id",
                schema: "hesap",
                table: "kullanici_rolleri",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "hesap",
                table: "kullanicilar",
                column: "normal_e_posta");

            migrationBuilder.CreateIndex(
                name: "ix_kullanicilar_uuid",
                schema: "hesap",
                table: "kullanicilar",
                column: "uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "hesap",
                table: "kullanicilar",
                column: "normal_kullanici_adi",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rol_iddialari_role_id",
                schema: "hesap",
                table: "rol_iddialari",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_rol_yetkileri_permission_id",
                schema: "hesap",
                table: "rol_yetkileri",
                column: "yetki_id");

            migrationBuilder.CreateIndex(
                name: "ix_rol_yetkileri_rol_id_yetki_id",
                schema: "hesap",
                table: "rol_yetkileri",
                columns: new[] { "rol_id", "yetki_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rol_yetkileri_uuid",
                schema: "hesap",
                table: "rol_yetkileri",
                column: "uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_roller_uuid",
                schema: "hesap",
                table: "roller",
                column: "uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "hesap",
                table: "roller",
                column: "normal_ad",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_yenileme_tokenlari_token_hash",
                schema: "hesap",
                table: "yenileme_tokenlari",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_yenileme_tokenlari_user_id",
                schema: "hesap",
                table: "yenileme_tokenlari",
                column: "kullanici_id");

            migrationBuilder.CreateIndex(
                name: "ix_yenileme_tokenlari_uuid",
                schema: "hesap",
                table: "yenileme_tokenlari",
                column: "uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_yetkiler_anahtar",
                schema: "hesap",
                table: "yetkiler",
                column: "anahtar",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_yetkiler_uuid",
                schema: "hesap",
                table: "yetkiler",
                column: "uuid",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "erisim_politikalari",
                schema: "hesap");

            migrationBuilder.DropTable(
                name: "kullanici_departmanlari",
                schema: "hesap");

            migrationBuilder.DropTable(
                name: "kullanici_girisleri",
                schema: "hesap");

            migrationBuilder.DropTable(
                name: "kullanici_grup_uyeleri",
                schema: "hesap");

            migrationBuilder.DropTable(
                name: "kullanici_iddialari",
                schema: "hesap");

            migrationBuilder.DropTable(
                name: "kullanici_rolleri",
                schema: "hesap");

            migrationBuilder.DropTable(
                name: "kullanici_tokenlari",
                schema: "hesap");

            migrationBuilder.DropTable(
                name: "rol_iddialari",
                schema: "hesap");

            migrationBuilder.DropTable(
                name: "rol_yetkileri",
                schema: "hesap");

            migrationBuilder.DropTable(
                name: "yenileme_tokenlari",
                schema: "hesap");

            migrationBuilder.DropTable(
                name: "departmanlar",
                schema: "hesap");

            migrationBuilder.DropTable(
                name: "kullanici_gruplari",
                schema: "hesap");

            migrationBuilder.DropTable(
                name: "roller",
                schema: "hesap");

            migrationBuilder.DropTable(
                name: "yetkiler",
                schema: "hesap");

            migrationBuilder.DropTable(
                name: "kullanicilar",
                schema: "hesap");
        }
    }
}
