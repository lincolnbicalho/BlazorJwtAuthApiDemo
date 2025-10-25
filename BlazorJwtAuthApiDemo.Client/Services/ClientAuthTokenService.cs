using BlazorJwtAuthApiDemo.Shared.Services;
using Microsoft.JSInterop;

namespace BlazorJwtAuthApiDemo.Client.Services;

/// <summary>
/// Client-side authentication token service for WebAssembly
/// Uses localStorage as the only storage mechanism (session/cookies not available in WASM)
///
/// Simpler than HybridAuthTokenService since WebAssembly always has JavaScript available
/// </summary>
public class ClientAuthTokenService : IAuthTokenService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<ClientAuthTokenService> _logger;

    public ClientAuthTokenService(
        IJSRuntime jsRuntime,
        ILogger<ClientAuthTokenService> logger)
    {
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    /// <summary>
    /// WebAssembly is never prerendering (always client-side)
    /// </summary>
    public bool IsPrerendering => false;

    public async ValueTask<string?> GetTokenAsync()
    {
        try
        {
            var token = await _jsRuntime.InvokeAsync<string?>(
                "localStorage.getItem", "auth_token");

            if (!string.IsNullOrEmpty(token))
            {
                _logger.LogDebug("Token retrieved from localStorage");
                return token;
            }

            _logger.LogDebug("No authentication token found");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving authentication token");
            return null;
        }
    }

    public async ValueTask<string?> GetRefreshTokenAsync()
    {
        try
        {
            var token = await _jsRuntime.InvokeAsync<string?>(
                "localStorage.getItem", "refresh_token");

            if (!string.IsNullOrEmpty(token))
            {
                _logger.LogDebug("Refresh token retrieved from localStorage");
                return token;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving refresh token");
            return null;
        }
    }

    public async ValueTask SetTokensAsync(string? token, string? refreshToken)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem",
                "auth_token", token ?? "");

            if (refreshToken != null)
            {
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem",
                    "refresh_token", refreshToken);
            }

            _logger.LogDebug("Tokens stored in localStorage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing authentication tokens");
        }
    }

    public async ValueTask ClearTokensAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "auth_token");
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "refresh_token");

            _logger.LogInformation("Authentication tokens cleared from localStorage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing authentication tokens");
        }
    }
}
