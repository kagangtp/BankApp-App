using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using IlkProjem.BLL.Interfaces;
using IlkProjem.Core.Enums;
using IlkProjem.Core.Exceptions;
using Microsoft.Extensions.Configuration;
using IlkProjem.Core.Dtos.AuthDtos;
using IlkProjem.Core.Models;
using IlkProjem.Core.Utilities.Results;
using Microsoft.Extensions.Localization;
using IlkProjem.Core.Resources;
using IlkProjem.DAL.Interfaces;

namespace IlkProjem.BLL.Services;

public class AuthService : IAuthService
{
    private readonly IConfiguration _configuration;
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IStringLocalizer<Messages> _localizer;

    public AuthService(
        IConfiguration configuration,
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IStringLocalizer<Messages> localizer)
    {
        _configuration = configuration;
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _localizer = localizer;
    }

    public async Task<IDataResult<LoginResponseDto>> Login(LoginDto loginDto, CancellationToken ct = default)
    {
        var user = await _userRepository.GetByEmailAsync(loginDto.Email, ct);

        if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
        {
            return new ErrorDataResult<LoginResponseDto>(_localizer["AuthError"]);
        }

        // Access token oluştur (15 dakika)
        var expiresAt = DateTime.UtcNow.AddMinutes(15);
        var accessToken = GenerateJwtToken(user, expiresAt);

        // Refresh token oluştur ve DB'ye kaydet
        var rawRefreshToken = GenerateRefreshToken();
        var hashedRefreshToken = HashToken(rawRefreshToken);

        var refreshTokenEntity = new RefreshToken
        {
            Token = hashedRefreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenExpiryDays()),
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.AddAsync(refreshTokenEntity, ct);
        await _refreshTokenRepository.SaveChangesAsync(ct);

        var responseDto = new LoginResponseDto
        {
            AccessToken = accessToken,
            ExpiresAt = expiresAt
        };

        // Controller'a hem DTO hem raw refresh token'ı döneceğiz
        // ama IDataResult<LoginResponseDto> sadece DTO taşıyor.
        // Raw refresh token'ı login-specific bir yolla iletmek gerekiyor.
        // Bunu iki adımda çözüyoruz: Login özel bir wrapper döner.
        return new LoginDataResult(responseDto, rawRefreshToken, refreshTokenEntity.ExpiresAt, _localizer["LoginSuccess"]);
    }

    public async Task<IResult> Register(RegisterDto registerDto, CancellationToken ct = default)
    {
        if (await _userRepository.ExistsByEmailAsync(registerDto.Email, ct))
        {
            return new ErrorResult(_localizer["EmailAlreadyExists"]);
        }

        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

        var newUser = new User
        {
            Username = registerDto.Username,
            Email = registerDto.Email,
            PasswordHash = hashedPassword,
            Role = "Staff",
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(newUser, ct);
        await _userRepository.SaveChangesAsync(ct);

        return new SuccessResult(_localizer["RegisterSuccess"]);
    }

    public async Task<IDataResult<(LoginResponseDto Response, string NewRefreshToken, DateTime RefreshTokenExpiry)>> RefreshToken(string rawRefreshToken, CancellationToken ct = default)
    {
        var hashedToken = HashToken(rawRefreshToken);
        var existingToken = await _refreshTokenRepository.GetByTokenAsync(hashedToken, ct);

        if (existingToken == null || !existingToken.IsActive)
        {
            return new ErrorDataResult<(LoginResponseDto, string, DateTime)>("Geçersiz veya süresi dolmuş refresh token.");
        }

        var user = existingToken.User;

        // Eski token'ı iptal et (Token Rotation)
        existingToken.RevokedAt = DateTime.UtcNow;

        // Yeni token çifti oluştur
        var newRawRefreshToken = GenerateRefreshToken();
        var newHashedRefreshToken = HashToken(newRawRefreshToken);

        // Rotation izlenebilirliği: eski token hangi yeni token ile değiştirildi
        existingToken.ReplacedByToken = newHashedRefreshToken;

        var newRefreshTokenEntity = new RefreshToken
        {
            Token = newHashedRefreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(GetRefreshTokenExpiryDays()),
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.AddAsync(newRefreshTokenEntity, ct);
        await _refreshTokenRepository.SaveChangesAsync(ct);

        // Yeni access token
        var expiresAt = DateTime.UtcNow.AddMinutes(15);
        var accessToken = GenerateJwtToken(user, expiresAt);

        var responseDto = new LoginResponseDto
        {
            AccessToken = accessToken,
            ExpiresAt = expiresAt
        };

        return new SuccessDataResult<(LoginResponseDto, string, DateTime)>(
            (responseDto, newRawRefreshToken, newRefreshTokenEntity.ExpiresAt),
            "Token yenilendi.");
    }

    public async Task<IResult> RevokeToken(string rawRefreshToken, CancellationToken ct = default)
    {
        var hashedToken = HashToken(rawRefreshToken);
        var existingToken = await _refreshTokenRepository.GetByTokenAsync(hashedToken, ct);

        if (existingToken == null || !existingToken.IsActive)
        {
            return new ErrorResult("Geçersiz token.");
        }

        existingToken.RevokedAt = DateTime.UtcNow;
        await _refreshTokenRepository.SaveChangesAsync(ct);

        return new SuccessResult("Token iptal edildi.");
    }

    // ---------- Private Helpers ----------

    private string GenerateJwtToken(User user, DateTime expiresAt)
    {
        var jwtKey = _configuration["Jwt:Key"] ?? throw new BusinessException(BusinessErrorCode.AuthJwtKeyMissing, "JWT Key is missing in configuration.");
        var key = Encoding.ASCII.GetBytes(jwtKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role ?? "No Role")
            }),
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"]
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Kriptografik olarak güvenli rastgele bir refresh token üretir
    /// </summary>
    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    /// <summary>
    /// Token'ı SHA256 ile hash'ler (DB'de plain-text saklamıyoruz)
    /// </summary>
    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(bytes);
    }

    private int GetRefreshTokenExpiryDays()
    {
        var value = _configuration["RefreshToken:ExpiresInDays"];
        return int.TryParse(value, out var days) ? days : 7;
    }
}

/// <summary>
/// Login'den hem DTO hem raw refresh token döndürmek için özel sonuç sınıfı.
/// Controller bu sınıfa cast ederek refresh token'a ulaşır.
/// </summary>
public class LoginDataResult : SuccessDataResult<LoginResponseDto>
{
    public string RawRefreshToken { get; }
    public DateTime RefreshTokenExpiry { get; }

    public LoginDataResult(LoginResponseDto data, string rawRefreshToken, DateTime refreshTokenExpiry, string message)
        : base(data, message)
    {
        RawRefreshToken = rawRefreshToken;
        RefreshTokenExpiry = refreshTokenExpiry;
    }
}