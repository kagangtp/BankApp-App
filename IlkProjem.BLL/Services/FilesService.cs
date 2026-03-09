using FileSignatures;
using IlkProjem.Core.Enums;
using IlkProjem.Core.Exceptions;
using IlkProjem.Core.Models;
using IlkProjem.Core.Dtos.FileDtos;
using IlkProjem.DAL.Interfaces;
using Microsoft.AspNetCore.Http;    
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace IlkProjem.BLL.Services;

public class FilesService : IFilesService
{
    private readonly IFilesRepository _fileRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly string _uploadRoot;

    public FilesService(IFilesRepository fileRepository, ICustomerRepository customerRepository, IConfiguration configuration)
    {
        _fileRepository = fileRepository;
        _customerRepository = customerRepository;
        
        // 1. ADIM: appsettings.json'daki yolu oku (Örn: /Users/kagan/Desktop/bankAppFiles)
        var pathFromConfig = configuration["FileSettings:StoragePath"];
        _uploadRoot = pathFromConfig ?? Path.Combine(Directory.GetCurrentDirectory(), "Uploads");

        if (!Directory.Exists(_uploadRoot))
            Directory.CreateDirectory(_uploadRoot);
    }

    public async Task<Files> UploadAsync(IFormFile file)
    {
        // 1. Boyut Kontrolü (Aynı kalıyor)
        const long maxFileSize = 5 * 1024 * 1024;
        if (file.Length > maxFileSize)
            throw new BusinessException(BusinessErrorCode.FileTooLarge, "Dosya çok büyük.");

        // 2. Hash Hesaplama (Aynı kalıyor)
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        using var stream = file.OpenReadStream();
        var hashBytes = await sha256.ComputeHashAsync(stream);
        var fileHash = Convert.ToHexString(hashBytes);

        // 3. Mükerrer Kontrolü (Deduplication Mantığı Değişiyor)
        var existingFile = await _fileRepository.GetByHashAsync(fileHash);
        string relativePath;
        string detectedMime;

        if (existingFile != null)
        {
            // Dosya zaten var! Fiziksel kayıt yapma, mevcut yolu kullan.
            relativePath = existingFile.RelativePath;
            detectedMime = existingFile.MimeType;
        }
        else
        {
            // Dosya yeni! Fiziksel olarak diskte oluştur.
            var inspector = new FileFormatInspector();
            stream.Position = 0;
            var format = inspector.DetermineFileFormat(stream);
            detectedMime = format?.MediaType ?? "application/octet-stream";

            var now = DateTime.UtcNow;
            var relativeFolder = Path.Combine(now.Year.ToString(), now.Month.ToString("D2"));
            var physicalFolder = Path.Combine(_uploadRoot, relativeFolder);

            if (!Directory.Exists(physicalFolder)) Directory.CreateDirectory(physicalFolder);

            var fileExtension = Path.GetExtension(file.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            relativePath = Path.Combine(relativeFolder, uniqueFileName);
            var physicalPath = Path.Combine(physicalFolder, uniqueFileName);

            using (var fileStream = new FileStream(physicalPath, FileMode.Create))
            {
                stream.Position = 0;
                await stream.CopyToAsync(fileStream);
            }
        }

        // KRİTİK NOKTA: Her durumda YENİ bir veritabanı kaydı oluşturuyoruz.
        // Böylece her yükleyenin kendi Id'si (Guid) ve kendi OwnerId'si olacak.
        var fileEntity = new Files
        {
            Id = Guid.NewGuid(), // Yeni kayıt
            FileName = file.FileName,
            MimeType = detectedMime,
            RelativePath = relativePath, // Ama aynı fiziksel yolu işaret ediyor
            FileSize = file.Length,
            FileHash = fileHash,
            Metadata = JsonSerializer.Serialize(new { OriginalName = file.FileName, Machine = Environment.MachineName }),
            CreatedAt = DateTime.UtcNow
        };

        await _fileRepository.AddAsync(fileEntity);
        await _fileRepository.SaveChangesAsync();

        return fileEntity;
    }

    public async Task<byte[]> DownloadAsync(Guid fileId)
    {
        // 2. ADIM: Veri tabanından dosya bilgisini bul
        var fileRecord = await _fileRepository.GetByIdAsync(fileId);
        if (fileRecord == null) throw new BusinessException(BusinessErrorCode.FileRecordNotFound, "Dosya kaydı bulunamadı.");

        // Fiziksel yolu inşa et
        var physicalPath = Path.Combine(_uploadRoot, fileRecord.RelativePath);

        if (!File.Exists(physicalPath))
            throw new BusinessException(BusinessErrorCode.FileNotFoundOnDisk, "Dosya diskte bulunamadı.");

        return await File.ReadAllBytesAsync(physicalPath);
    }

    public async Task<bool> DeleteAsync(Guid fileId)
    {
        // 1. Veri tabanından silinecek kaydı bul
        var fileRecord = await _fileRepository.GetByIdAsync(fileId);
        if (fileRecord == null) return false;

        // 2. KRİTİK ADIM: Bu fiziksel yolu (RelativePath) kullanan toplam kaç kayıt var?
        // Not: Bu metodu aşağıda Repository kısmında ekleyeceğiz.
        var usageCount = await _fileRepository.CountByPathAsync(fileRecord.RelativePath);

        // 3. Eğer bu dosyayı kullanan tek kişi bizsek (veya son kişi kalmışsak), diskten sil.
        if (usageCount <= 1)
        {
            var physicalPath = Path.Combine(_uploadRoot, fileRecord.RelativePath);
            if (File.Exists(physicalPath))
                File.Delete(physicalPath);
        }
        // usageCount > 1 ise: Dosyayı diskten silmiyoruz çünkü başkaları hala ona bakıyor!

        // 4. Veritabanı kaydını (pointer) her zaman siliyoruz.
        _fileRepository.Delete(fileRecord);
        await _fileRepository.SaveChangesAsync();

        return true;
    }

    public async Task<bool> AssignOwnerAsync(Guid fileId, FileAssignDto assignDto)
    {
        var file = await _fileRepository.GetByIdAsync(fileId);
        if (file == null) return false;

        file.OwnerId = assignDto.OwnerId;
        file.OwnerType = assignDto.OwnerType;

        _fileRepository.Update(file);

        // If assigning to a Customer, update their ProfileImageId
        if (assignDto.OwnerType == "Customer")
        {
            var customer = await _customerRepository.GetByIdAsync(assignDto.OwnerId);
            if (customer != null)
            {
                customer.ProfileImageId = fileId;
                await _customerRepository.UpdateAsync(customer);
            }
        }

        await _fileRepository.SaveChangesAsync();
        return true;
    }

    public async Task<List<FileReadDto>> GetByOwnerAsync(string ownerType, int ownerId)
    {
        var files = await _fileRepository.GetByOwnerAsync(ownerType, ownerId);
        return files.Select(f => new FileReadDto
        {
            Id = f.Id,
            FileName = f.FileName,
            MimeType = f.MimeType,
            RelativePath = f.RelativePath
        }).ToList();
    }

    public async Task<Files?> GetFileRecordAsync(Guid fileId)
    {
        return await _fileRepository.GetByIdAsync(fileId);
    }
}