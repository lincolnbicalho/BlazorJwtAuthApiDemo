using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BlazorJwtAuthApiDemo.Shared.Services;

namespace BlazorJwtAuthApiDemo.Client.Services;

/// <summary>
/// WebAssembly implementation of IApiService for HTTP communication with the API
/// Uses HttpClient configured with ApiAuthorizationMessageHandler for automatic JWT injection
/// </summary>
public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<ApiService> _logger;

    public ApiService(
        HttpClient httpClient,
        ILogger<ApiService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        // WHY: Configure JSON serialization options to match API expectations
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
        };
    }

    public async Task<TResponse?> GetAsync<TResponse>(string endpoint, string? jwtToken = null)
    {
        try
        {
            // NOTE: jwtToken parameter is ignored in WASM because ApiAuthorizationMessageHandler
            // automatically handles JWT injection from localStorage

            _logger.LogDebug("GET {Endpoint}", endpoint);

            var response = await _httpClient.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
                _logger.LogDebug("GET {Endpoint} succeeded", endpoint);
                return result;
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
            // NOTE: jwtToken parameter is ignored in WASM because ApiAuthorizationMessageHandler
            // automatically handles JWT injection from localStorage

            _logger.LogDebug("POST {Endpoint}", endpoint);

            var response = await _httpClient.PostAsJsonAsync(endpoint, data, _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
                _logger.LogDebug("POST {Endpoint} succeeded", endpoint);
                return result;
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
            _logger.LogDebug("PUT {Endpoint}", endpoint);

            var response = await _httpClient.PutAsJsonAsync(endpoint, data, _jsonOptions);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<TResponse>(_jsonOptions);
                _logger.LogDebug("PUT {Endpoint} succeeded", endpoint);
                return result;
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
            _logger.LogDebug("DELETE {Endpoint}", endpoint);

            var response = await _httpClient.DeleteAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("DELETE {Endpoint} succeeded", endpoint);
                return true;
            }

            _logger.LogWarning("DELETE {Endpoint} failed: {StatusCode}",
                endpoint, response.StatusCode);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DELETE {Endpoint} exception", endpoint);
            throw;
        }
    }
}
