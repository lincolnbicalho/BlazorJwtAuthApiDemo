namespace BlazorJwtAuthApiDemo.Shared.Services;

/// <summary>
/// Service for making HTTP requests to the API with automatic JWT token handling
/// Based on production implementation from Bliik.Therapists
/// </summary>
public interface IApiService
{
    /// <summary>
    /// Sends a GET request to the API
    /// </summary>
    /// <typeparam name="TResponse">Expected response type</typeparam>
    /// <param name="endpoint">API endpoint (e.g., "api/auth/me")</param>
    /// <param name="jwtToken">Optional JWT token to override automatic detection</param>
    /// <returns>Deserialized response or default</returns>
    Task<TResponse?> GetAsync<TResponse>(string endpoint, string? jwtToken = null);

    /// <summary>
    /// Sends a POST request to the API
    /// </summary>
    /// <typeparam name="TRequest">Request body type</typeparam>
    /// <typeparam name="TResponse">Expected response type</typeparam>
    /// <param name="endpoint">API endpoint</param>
    /// <param name="data">Request body data</param>
    /// <param name="jwtToken">Optional JWT token to override automatic detection</param>
    /// <returns>Deserialized response or default</returns>
    Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest data, string? jwtToken = null);

    /// <summary>
    /// Sends a PUT request to the API
    /// </summary>
    /// <typeparam name="TRequest">Request body type</typeparam>
    /// <typeparam name="TResponse">Expected response type</typeparam>
    /// <param name="endpoint">API endpoint</param>
    /// <param name="data">Request body data</param>
    /// <returns>Deserialized response or default</returns>
    Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data);

    /// <summary>
    /// Sends a DELETE request to the API
    /// </summary>
    /// <param name="endpoint">API endpoint</param>
    /// <returns>True if successful, false otherwise</returns>
    Task<bool> DeleteAsync(string endpoint);
}
