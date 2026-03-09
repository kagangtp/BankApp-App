// Core/Enums/BusinessErrorCodes.cs
namespace IlkProjem.Core.Enums;
public enum BusinessErrorCode
{
    // =============================================
    // Format: KatmanId(1) + ServisId(2) + MetodId(2)
    // BLL = 1xxx, DAL = 9xxx
    // =============================================

    None = 0,

    // ─── Auth Servis (10) ───────────────────────
    AuthInvalidCredentials   = 1001, // Login: E-posta veya şifre hatalı
    AuthEmailAlreadyExists   = 1002, // Register: Bu e-posta zaten kayıtlı
    AuthInvalidRefreshToken  = 1003, // RefreshToken: Geçersiz veya süresi dolmuş
    AuthJwtKeyMissing        = 1004, // GenerateJwtToken: appsettings'te JWT Key yok
    AuthRevokeTokenFailed    = 1005, // RevokeToken: Token bulunamadı veya zaten iptal

    // ─── Customer Servis (20) ───────────────────
    CustomerNotFound         = 1201, // GetById / Update: Müşteri bulunamadı
    CustomerDeleteFailed     = 1202, // Delete: Silme işlemi başarısız

    // ─── Car Servis (30) ────────────────────────
    CarNotFound              = 1301, // GetById / Update: Araba bulunamadı
    CarUpdateFailed          = 1302, // Update: Güncelleme başarısız
    CarDeleteFailed          = 1303, // Delete: Silme başarısız

    // ─── House Servis (40) ──────────────────────
    HouseNotFound            = 1401, // GetById / Update: Ev bulunamadı
    HouseUpdateFailed        = 1402, // Update: Güncelleme başarısız
    HouseDeleteFailed        = 1403, // Delete: Silme başarısız

    // ─── Files Servis (50) ──────────────────────
    FileRecordNotFound       = 1501, // Download/Delete: Dosya kaydı DB'de bulunamadı
    FileNotFoundOnDisk       = 1502, // Download: Dosya diskte bulunamadı
    FileAssignFailed         = 1503, // AssignOwner: Dosya ataması başarısız
    FileTooLarge             = 1504, // Upload: Dosya boyutu limitin üzerinde (YENİ)
    DuplicateFile            = 1505, // Upload: Aynı hash değerine sahip dosya mevcut (YENİ)

    // ─── Calculator Servis (60) ─────────────────
    CalculatorDivideByZero   = 1601, // Divide: Sıfıra bölme hatası

    // ─── Mail Servis (70) ───────────────────────
    MailSendFailed           = 1701, // SendMail: Mail gönderme başarısız
    MailNotImplemented       = 1702, // SendMailAsync1: Henüz implemente edilmedi

    // ─── Excel Servis (80) ──────────────────────
    ExcelGenerationFailed    = 1801, // GenerateExcel: Excel oluşturma hatası

    // ─── Veri Tabanı Hataları (DAL - 99) ────────
    DatabaseSaveFailed       = 9901, // Genel DB kaydetme hatası
    DatabaseConnectionFailed = 9902  // DB bağlantı hatası
}