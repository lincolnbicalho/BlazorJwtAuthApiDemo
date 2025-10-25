namespace BlazorJwtAuthApiDemo.Api.Services;

/// <summary>
/// Service for generating and validating JWT tokens
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a JWT access token for the given user
    /// </summary>
    /// <param name="userId">User's unique identifier</param>
    /// <param name="email">User's email address</param>
    /// <param name="roles">User's roles for authorization</param>
    /// <returns>JWT token string</returns>
    string GenerateAccessToken(Guid userId, string email, IList<string> roles);

    /// <summary>
    /// Generates a refresh token for token renewal
    /// </summary>
    /// <returns>Refresh token string</returns>
    string GenerateRefreshToken();

    /// <summary>
    /// Validates a JWT token
    /// </summary>
    /// <param name="token">JWT token to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    Task<bool> ValidateTokenAsync(string token);

    /// <summary>
    /// Extracts the user ID from a valid JWT token
    /// </summary>
    /// <param name="token">JWT token</param>
    /// <returns>User ID if valid, null otherwise</returns>
    Task<Guid?> GetUserIdFromTokenAsync(string token);
}
