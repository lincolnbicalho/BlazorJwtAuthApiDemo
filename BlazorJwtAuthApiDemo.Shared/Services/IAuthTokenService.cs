namespace BlazorJwtAuthApiDemo.Shared.Services;

/// <summary>
/// Interface for authentication token management across all render modes
/// Supports hybrid storage strategy: Session + Cookie + localStorage
///
/// This interface is based on the production pattern described in:
/// "Solving Blazor 8's Authentication Crisis" - https://ljblab.dev/blog
/// </summary>
public interface IAuthTokenService
{
    /// <summary>
    /// Gets the authentication token from available storage
    /// Tries Session -> Cookie -> localStorage based on context
    /// </summary>
    /// <returns>The JWT access token or null if not found</returns>
    ValueTask<string?> GetTokenAsync();

    /// <summary>
    /// Gets the refresh token from available storage
    /// </summary>
    /// <returns>The JWT refresh token or null if not found</returns>
    ValueTask<string?> GetRefreshTokenAsync();

    /// <summary>
    /// Stores authentication tokens in all available storage mechanisms
    /// - Session (server-side, fastest)
    /// - Secure cookies (persistent, cross-mode)
    /// - localStorage (client-side, WASM)
    /// </summary>
    /// <param name="token">The JWT access token</param>
    /// <param name="refreshToken">The JWT refresh token (optional)</param>
    ValueTask SetTokensAsync(string? token, string? refreshToken);

    /// <summary>
    /// Clears all authentication tokens from all storage mechanisms
    /// Used during logout or when tokens expire
    /// </summary>
    ValueTask ClearTokensAsync();

    /// <summary>
    /// Indicates whether the current execution is during server-side prerendering
    /// Important: JavaScript interop is NOT available during prerendering
    /// </summary>
    bool IsPrerendering { get; }
}
