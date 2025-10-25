using BlazorJwtAuthApiDemo.Shared.Models;

namespace BlazorJwtAuthApiDemo.Services;

/// <summary>
/// Service for authentication operations
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates a user and signs them in
    /// </summary>
    /// <param name="httpContext">HTTP context for cookie authentication</param>
    /// <param name="model">Login credentials</param>
    /// <returns>Login response with JWT token and user info</returns>
    Task<LoginResponse?> LoginAsync(HttpContext httpContext, LoginModel model);

    /// <summary>
    /// Registers a new user and signs them in
    /// </summary>
    /// <param name="httpContext">HTTP context for cookie authentication</param>
    /// <param name="model">Registration information</param>
    /// <returns>Login response with JWT token and user info</returns>
    Task<LoginResponse?> RegisterAsync(HttpContext httpContext, RegisterModel model);

    /// <summary>
    /// Signs the user out and clears authentication
    /// </summary>
    /// <param name="httpContext">HTTP context for cookie authentication</param>
    /// <returns>True if successful</returns>
    Task<bool> LogoutAsync(HttpContext httpContext);
}
