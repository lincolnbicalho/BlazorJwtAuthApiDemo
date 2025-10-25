namespace BlazorJwtAuthApiDemo.Shared.Models;

/// <summary>
/// Response model for successful login
/// Contains JWT token and user information
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// JWT access token for API authentication
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token for renewing access tokens (future implementation)
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// User information
    /// </summary>
    public UserModel User { get; set; } = new();

    /// <summary>
    /// Token expiration time
    /// </summary>
    public DateTime ExpiresAt { get; set; }
}
