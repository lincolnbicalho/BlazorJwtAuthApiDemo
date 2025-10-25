using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Json;

namespace BlazorJwtAuthApiDemo.Shared.Services;

/// <summary>
/// Custom authentication state provider that parses JWT tokens manually
/// Works across all render modes: SSR, Server, WebAssembly, and Auto
///
/// Based on production implementation from "Solving Blazor 8's Authentication Crisis"
/// https://ljblab.dev/blog/solving-blazor-8-authentication-crisis
/// </summary>
public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly IAuthTokenService _tokenService;
    private readonly ILogger<CustomAuthStateProvider> _logger;

    public CustomAuthStateProvider(
        IAuthTokenService tokenService,
        ILogger<CustomAuthStateProvider> logger)
    {
        _tokenService = tokenService;
        _logger = logger;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            // STEP 1: Get token from hybrid storage
            var token = await _tokenService.GetTokenAsync();

            if (string.IsNullOrEmpty(token))
            {
                _logger.LogDebug("No token found, returning anonymous user");
                return new AuthenticationState(
                    new ClaimsPrincipal(new ClaimsIdentity()));
            }

            // STEP 2: Parse JWT without external libraries
            // WHY: Reduces dependencies and improves performance
            // HOW: Manual base64 decoding of JWT payload
            var claims = ParseClaimsFromJwt(token).ToList();

            // IMPORTANT: Add the raw JWT token as a claim for TokenBridge
            // WHY: TokenBridge needs the raw JWT to transfer to localStorage for WASM
            // HOW: Add as "jwt" claim so TokenBridge can find it
            claims.Add(new Claim("jwt", token));

            // STEP 3: Check token expiration
            // WHY: Prevents using expired tokens
            // HOW: Check 'exp' claim against current time
            var expClaim = claims.FirstOrDefault(c => c.Type == "exp");
            if (expClaim != null)
            {
                var expTime = DateTimeOffset.FromUnixTimeSeconds(
                    long.Parse(expClaim.Value));

                if (expTime < DateTimeOffset.UtcNow)
                {
                    _logger.LogWarning("Token expired at {ExpirationTime}", expTime);
                    await _tokenService.ClearTokensAsync();
                    return new AuthenticationState(
                        new ClaimsPrincipal(new ClaimsIdentity()));
                }
            }

            // STEP 4: Create authenticated user
            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);

            _logger.LogDebug("User authenticated: {UserName}",
                user.Identity?.Name ?? "Unknown");

            return new AuthenticationState(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting authentication state");
            return new AuthenticationState(
                new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    /// <summary>
    /// Parse JWT claims manually without external dependencies
    /// WHY: Avoid bringing in heavy JWT libraries for simple parsing
    /// HOW: Split token, decode base64 payload, deserialize JSON
    /// </summary>
    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var payload = jwt.Split('.')[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

        var claims = new List<Claim>();
        if (keyValuePairs != null)
        {
            foreach (var kvp in keyValuePairs)
            {
                // Handle array claims (like roles)
                if (kvp.Value is JsonElement element)
                {
                    if (element.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in element.EnumerateArray())
                        {
                            claims.Add(new Claim(kvp.Key, item.ToString()));
                        }
                    }
                    else
                    {
                        claims.Add(new Claim(kvp.Key, element.ToString()));
                    }
                }
                else
                {
                    claims.Add(new Claim(kvp.Key, kvp.Value?.ToString() ?? ""));
                }
            }

            // Map common JWT claims to ClaimTypes
            MapStandardClaims(claims);
        }

        return claims;
    }

    /// <summary>
    /// JWT base64 encoding doesn't include padding characters
    /// WHY: JWT spec uses base64url encoding without padding
    /// HOW: Add padding based on string length modulo 4
    /// </summary>
    private byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }

    /// <summary>
    /// Map standard JWT claims to ASP.NET Core ClaimTypes
    /// This ensures proper integration with Blazor's authorization system
    /// </summary>
    private void MapStandardClaims(List<Claim> claims)
    {
        // Map 'sub' to NameIdentifier
        var subClaim = claims.FirstOrDefault(c => c.Type == "sub");
        if (subClaim != null && !claims.Any(c => c.Type == ClaimTypes.NameIdentifier))
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, subClaim.Value));
        }

        // Map 'email' to Email
        var emailClaim = claims.FirstOrDefault(c => c.Type == "email");
        if (emailClaim != null && !claims.Any(c => c.Type == ClaimTypes.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, emailClaim.Value));
        }

        // Map 'name' to Name
        var nameClaim = claims.FirstOrDefault(c => c.Type == "name");
        if (nameClaim != null && !claims.Any(c => c.Type == ClaimTypes.Name))
        {
            claims.Add(new Claim(ClaimTypes.Name, nameClaim.Value));
        }

        // Map 'role' to Role (already handled in ParseClaimsFromJwt for arrays)
        var roleClaims = claims.Where(c => c.Type == "role").ToList();
        foreach (var roleClaim in roleClaims)
        {
            if (!claims.Any(c => c.Type == ClaimTypes.Role && c.Value == roleClaim.Value))
            {
                claims.Add(new Claim(ClaimTypes.Role, roleClaim.Value));
            }
        }
    }

    /// <summary>
    /// Notify Blazor that authentication state has changed
    /// WHY: Allow components to trigger authentication state refresh
    /// HOW: Call this after login/logout operations
    /// </summary>
    public void NotifyAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}
