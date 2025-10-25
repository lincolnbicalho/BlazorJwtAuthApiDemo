using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using BlazorJwtAuthApiDemo.Shared.Models;
using BlazorJwtAuthApiDemo.Shared.Services;

namespace BlazorJwtAuthApiDemo.Services;

/// <summary>
/// Implementation of IAuthService for authentication operations
/// Based on production implementation from Bliik.Therapists
///
/// KEY CONCEPT: This service calls the API to get JWT tokens,
/// then stores the JWT in multiple locations:
/// - Cookie claims (for Server components)
/// - Session (for fast retrieval)
/// - localStorage (for WebAssembly components)
/// </summary>
public class AuthService : IAuthService
{
    private readonly IApiService _apiService;
    private readonly IAuthTokenService _tokenService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IApiService apiService,
        IAuthTokenService tokenService,
        ILogger<AuthService> logger)
    {
        _apiService = apiService;
        _tokenService = tokenService;
        _logger = logger;
    }

    public async Task<LoginResponse?> LoginAsync(HttpContext httpContext, LoginModel model)
    {
        try
        {
            _logger.LogInformation("Attempting login for {Email}", model.Email);

            // STEP 1: Call API to authenticate and get JWT token
            var response = await _apiService.PostAsync<LoginModel, LoginResponse>(
                "api/auth/login",
                model);

            if (response == null)
            {
                _logger.LogWarning("Login failed: No response from API");
                return null;
            }

            // STEP 2: Create claims identity for cookie authentication
            // WHY: Store user information and JWT token as claims
            // HOW: Cookie authentication will encrypt and store these claims
            var claims = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);

            claims.AddClaim(new Claim(ClaimTypes.Sid, response.User.Id.ToString()));
            claims.AddClaim(new Claim(ClaimTypes.NameIdentifier, response.User.Id.ToString()));
            claims.AddClaim(new Claim(ClaimTypes.Name, response.User.FirstName ?? string.Empty));
            claims.AddClaim(new Claim(ClaimTypes.Email, response.User.Email ?? string.Empty));

            // WHY: Store JWT token as a claim so ApiService can extract it
            // HOW: The claim name "jwt" is what ApiService looks for
            claims.AddClaim(new Claim("jwt", response.AccessToken ?? string.Empty));

            // WHY: Store roles for authorization
            foreach (var role in response.User.Roles)
            {
                claims.AddClaim(new Claim(ClaimTypes.Role, role));
            }

            var claimsPrincipal = new ClaimsPrincipal(claims);

            // STEP 3: Sign in with cookie authentication
            // WHY: This creates an encrypted cookie that persists the authentication
            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                claimsPrincipal,
                new AuthenticationProperties
                {
                    IsPersistent = true, // Remember me across browser sessions
                    IssuedUtc = DateTimeOffset.UtcNow,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1)
                });

            // STEP 4: Store tokens using HybridAuthTokenService
            // WHY: This makes tokens available to WebAssembly components via localStorage
            // HOW: HybridAuthTokenService stores in Session + Cookie + localStorage
            await _tokenService.SetTokensAsync(
                response.AccessToken,
                response.RefreshToken);

            _logger.LogInformation("Login successful for {Email}", model.Email);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for {Email}", model.Email);
            throw;
        }
    }

    public async Task<LoginResponse?> RegisterAsync(HttpContext httpContext, RegisterModel model)
    {
        try
        {
            _logger.LogInformation("Attempting registration for {Email}", model.Email);

            // STEP 1: Call API to register user and get JWT token
            var response = await _apiService.PostAsync<RegisterModel, LoginResponse>(
                "api/auth/register",
                model);

            if (response == null)
            {
                _logger.LogWarning("Registration failed: No response from API");
                return null;
            }

            // STEP 2: Same as login - create claims and sign in
            var claims = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);

            claims.AddClaim(new Claim(ClaimTypes.Sid, response.User.Id.ToString()));
            claims.AddClaim(new Claim(ClaimTypes.NameIdentifier, response.User.Id.ToString()));
            claims.AddClaim(new Claim(ClaimTypes.Name, response.User.FirstName ?? string.Empty));
            claims.AddClaim(new Claim(ClaimTypes.Email, response.User.Email ?? string.Empty));
            claims.AddClaim(new Claim("jwt", response.AccessToken ?? string.Empty));

            foreach (var role in response.User.Roles)
            {
                claims.AddClaim(new Claim(ClaimTypes.Role, role));
            }

            var claimsPrincipal = new ClaimsPrincipal(claims);

            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                claimsPrincipal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    IssuedUtc = DateTimeOffset.UtcNow,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1)
                });

            // Store tokens using HybridAuthTokenService (same as login)
            await _tokenService.SetTokensAsync(
                response.AccessToken,
                response.RefreshToken);

            _logger.LogInformation("Registration successful for {Email}", model.Email);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for {Email}", model.Email);
            throw;
        }
    }

    public async Task<bool> LogoutAsync(HttpContext httpContext)
    {
        try
        {
            // STEP 1: Sign out from cookie authentication
            // WHY: This removes the authentication cookie
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // STEP 2: Clear tokens from all storage locations
            // WHY: This removes tokens from Session + Cookie + localStorage
            // HOW: HybridAuthTokenService clears all storage mechanisms
            await _tokenService.ClearTokensAsync();

            _logger.LogInformation("Logout successful");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return false;
        }
    }
}
