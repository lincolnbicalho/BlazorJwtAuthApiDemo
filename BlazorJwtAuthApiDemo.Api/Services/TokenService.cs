using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace BlazorJwtAuthApiDemo.Api.Services;

/// <summary>
/// Implementation of ITokenService for JWT token operations
/// Based on production implementation from Bliik.Therapists
/// </summary>
public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly SymmetricSecurityKey _key;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;

        // WHY: Get secret key from configuration for JWT signing
        // HOW: Symmetric key ensures both signing and validation use the same key
        var secretKey = _configuration["JWT:SecretKey"]
            ?? throw new ArgumentNullException("JWT:SecretKey", "JWT Secret Key is required in configuration");

        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    }

    public string GenerateAccessToken(Guid userId, string email, IList<string> roles)
    {
        // WHY: Build claims that will be encoded in the JWT token
        // HOW: Claims provide user identity and authorization information
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // Unique token ID
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64) // Issued at
        };

        // WHY: Add roles for authorization checks
        // HOW: Each role becomes a separate claim
        if (roles == null)
            throw new ArgumentNullException(nameof(roles));

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // WHY: Configure signing credentials using HMAC-SHA256
        // HOW: This ensures the token cannot be tampered with
        var credentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);

        // WHY: Get configuration values for token validation
        var issuer = _configuration["JWT:Issuer"];
        var audience = _configuration["JWT:Audience"];
        var expireMinutes = int.Parse(_configuration["JWT:ExpireMinutes"] ?? "60");

        // WHY: Create the JWT security token with all configuration
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expireMinutes),
            signingCredentials: credentials
        );

        // WHY: Serialize the token to a string that can be sent to clients
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        // WHY: Generate a cryptographically secure random token
        // HOW: Using RandomNumberGenerator for strong randomness
        var randomNumber = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = GetTokenValidationParameters();

            // WHY: Validate the token against our security parameters
            var result = await tokenHandler.ValidateTokenAsync(token, validationParameters);
            return result.IsValid;
        }
        catch
        {
            // WHY: Any exception during validation means the token is invalid
            return false;
        }
    }

    public async Task<Guid?> GetUserIdFromTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParameters = GetTokenValidationParameters();

            // WHY: Validate and extract claims from the token
            var result = await tokenHandler.ValidateTokenAsync(token, validationParameters);
            if (!result.IsValid)
                return null;

            // WHY: Extract the user ID from the NameIdentifier claim
            var userIdClaim = result.ClaimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets token validation parameters matching the generation configuration
    /// </summary>
    private TokenValidationParameters GetTokenValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _key,
            ValidateIssuer = true,
            ValidIssuer = _configuration["JWT:Issuer"],
            ValidateAudience = true,
            ValidAudience = _configuration["JWT:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero // WHY: No grace period for token expiration
        };
    }
}
