using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using BlazorJwtAuthApiDemo.Shared.Services;

namespace BlazorJwtAuthApiDemo.Services;

/// <summary>
/// Implementation of IApiService for HTTP communication with the API
/// Automatically handles JWT token extraction and injection
/// Based on production implementation from Bliik.Therapists
/// </summary>
public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ILogger<ApiService> _logger;

    public ApiService(
        HttpClient httpClient,
        IConfiguration configuration,
        AuthenticationStateProvider authStateProvider,
        ILogger<ApiService> logger)
    {
        _httpClient = httpClient;
        _authStateProvider = authStateProvider;
        _logger = logger;

        // WHY: Configure HttpClient base address from configuration
        _httpClient.BaseAddress = new Uri(configuration["ApiBaseUrl"] ?? "https://localhost:7230");

        // WHY: Configure JSON serialization options
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
    }

    /// <summary>
    /// Sets the Authorization header with the JWT token
    /// </summary>
    /// <param name="providedJwtToken">Optional token to use instead of extracting from auth state</param>
    private async Task SetAuthorizationHeaderAsync(string? providedJwtToken = null)
    {
        try
        {
            // WHY: If JWT token is provided directly, use it (useful during login)
            if (!string.IsNullOrEmpty(providedJwtToken))
            {
                _httpClient.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", providedJwtToken);
                _logger.LogDebug("JWT token set from provided parameter");
                return;
            }

            // WHY: Otherwise, extract JWT from authentication state
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            if (authState.User.Identity?.IsAuthenticated == true)
            {
                // WHY: The JWT is stored as a claim named "jwt" during login
                var jwtToken = authState.User.FindFirst("jwt")?.Value;
                if (!string.IsNullOrEmpty(jwtToken))
                {
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", jwtToken);
                    _logger.LogDebug("JWT token set from authentication state");
                }
                else
                {
                    _logger.LogWarning("No JWT token found in user claims");
                }
            }
            else
            {
                _logger.LogDebug("User not authenticated");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting authorization header");
        }
    }

    public async Task<TResponse?> GetAsync<TResponse>(string endpoint, string? jwtToken = null)
    {
        try
        {
            await SetAuthorizationHeaderAsync(jwtToken);
            var response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TResponse>(json, _jsonOptions);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("GET {Endpoint} failed: {StatusCode} - {Error}",
                endpoint, response.StatusCode, errorContent);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GET {Endpoint} exception", endpoint);
            throw;
        }
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string endpoint,
        TRequest data,
        string? jwtToken = null)
    {
        try
        {
            await SetAuthorizationHeaderAsync(jwtToken);
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogDebug("POST {Endpoint} - Data: {Data}", endpoint, json);

            var response = await _httpClient.PostAsync(endpoint, content);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TResponse>(responseJson, _jsonOptions);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("POST {Endpoint} failed: {StatusCode} - {Error}",
                endpoint, response.StatusCode, errorContent);
            return default;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "POST {Endpoint} exception", endpoint);
            throw;
        }
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest data)
    {
        try
        {
            await SetAuthorizationHeaderAsync();
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogDebug("PUT {Endpoint} - Data: {Data}", endpoint, json);

            var response = await _httpClient.PutAsync(endpoint, content);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("PUT {Endpoint} success - Response: {Response}",
                    endpoint, responseJson);
                return JsonSerializer.Deserialize<TResponse>(responseJson, _jsonOptions);
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("PUT {Endpoint} failed: {StatusCode} - {Error}",
                endpoint, response.StatusCode, errorContent);

            throw new HttpRequestException(
                $"PUT {endpoint} failed with status {response.StatusCode}: {errorContent}",
                null,
                response.StatusCode);
        }
        catch (HttpRequestException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PUT {Endpoint} exception", endpoint);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(string endpoint)
    {
        try
        {
            await SetAuthorizationHeaderAsync();
            var response = await _httpClient.DeleteAsync(endpoint);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DELETE {Endpoint} exception", endpoint);
            throw;
        }
    }
}
