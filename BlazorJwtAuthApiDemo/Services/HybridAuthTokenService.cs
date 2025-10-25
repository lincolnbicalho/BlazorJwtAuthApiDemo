using BlazorJwtAuthApiDemo.Shared.Services;
using Microsoft.JSInterop;

namespace BlazorJwtAuthApiDemo.Services;

/// <summary>
/// Hybrid authentication token service implementing multi-layered storage strategy
/// Storage hierarchy: Session (fastest) -> Cookie (persistent) -> localStorage (client-side)
///
/// Based on production implementation from "Solving Blazor 8's Authentication Crisis"
/// https://ljblab.dev/blog/solving-blazor-8-authentication-crisis
/// </summary>
public class HybridAuthTokenService : IAuthTokenService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<HybridAuthTokenService> _logger;

    public HybridAuthTokenService(
        IHttpContextAccessor httpContextAccessor,
        IJSRuntime jsRuntime,
        ILogger<HybridAuthTokenService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    /// <summary>
    /// Check if we're currently in server-side prerendering mode
    /// During prerendering, Response.HasStarted is false and JavaScript is not available
    /// </summary>
    public bool IsPrerendering =>
        _httpContextAccessor.HttpContext?.Response.HasStarted == false;

    public async ValueTask<string?> GetTokenAsync()
    {
        try
        {
            // STEP 1: Try server-side storage first (fastest, most reliable)
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                // WHY: Check session first - it's the fastest option
                // HOW: Session is available immediately on server
                var sessionToken = context.Session.GetString("auth_token");
                if (!string.IsNullOrEmpty(sessionToken))
                {
                    _logger.LogDebug("Token retrieved from session");
                    return sessionToken;
                }

                // WHY: Check secure cookie as fallback - persists across requests
                // HOW: Cookies are sent with every request automatically
                if (context.Request.Cookies.TryGetValue("auth_token", out var cookieToken))
                {
                    _logger.LogDebug("Token retrieved from cookie");
                    return cookieToken;
                }
            }

            // STEP 2: If not prerendering, try client-side storage
            // WHY: WebAssembly mode might have token in localStorage
            // HOW: Check if JavaScript runtime is available first
            // NOTE: Works in both Blazor Server (via SignalR) and WebAssembly (direct)
            if (!IsPrerendering)
            {
                try
                {
                    var localToken = await _jsRuntime.InvokeAsync<string?>(
                        "localStorage.getItem", "auth_token");

                    if (!string.IsNullOrEmpty(localToken))
                    {
                        _logger.LogDebug("Token retrieved from localStorage");
                        return localToken;
                    }
                }
                catch (InvalidOperationException ex)
                {
                    // WHY: JavaScript interop might not be available yet
                    // HOW: Fall back to server storage gracefully
                    _logger.LogDebug(ex, "JS interop not available, using server storage");
                }
            }

            _logger.LogWarning("No authentication token found in any storage");
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
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                // WHY: Same pattern as GetTokenAsync for consistency
                var sessionToken = context.Session.GetString("refresh_token");
                if (!string.IsNullOrEmpty(sessionToken))
                    return sessionToken;

                if (context.Request.Cookies.TryGetValue("refresh_token", out var cookieToken))
                    return cookieToken;
            }

            // NOTE: Works in both Blazor Server (via SignalR) and WebAssembly (direct)
            if (!IsPrerendering)
            {
                try
                {
                    return await _jsRuntime.InvokeAsync<string?>(
                        "localStorage.getItem", "refresh_token");
                }
                catch (InvalidOperationException)
                {
                    _logger.LogDebug("JS interop not available for refresh token");
                }
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
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            _logger.LogWarning("HttpContext not available for token storage");
            return;
        }

        try
        {
            // STEP 1: Store in session if response hasn't started
            // WHY: Session is fastest for subsequent requests in same circuit
            // HOW: Check Response.HasStarted to avoid exceptions
            if (!context.Response.HasStarted)
            {
                context.Session.SetString("auth_token", token ?? "");
                if (refreshToken != null)
                    context.Session.SetString("refresh_token", refreshToken);

                _logger.LogDebug("Tokens stored in session");
            }
            else
            {
                _logger.LogDebug("Response started, skipping session storage");
            }

            // STEP 2: Always set secure cookie for persistence
            // WHY: Cookies persist across server restarts and browser refresh
            // HOW: Use strict security settings for token protection
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,      // Prevents JavaScript access
                Secure = true,        // HTTPS only
                SameSite = SameSiteMode.Strict,  // CSRF protection
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            };

            context.Response.Cookies.Append("auth_token", token ?? "", cookieOptions);
            if (refreshToken != null)
                context.Response.Cookies.Append("refresh_token", refreshToken, cookieOptions);

            _logger.LogDebug("Tokens stored in cookies");

            // STEP 3: Store in localStorage if available (for WASM scenarios)
            // WHY: WebAssembly components need client-side token access
            // HOW: Safely attempt JS interop with exception handling
            // NOTE: Works in both Blazor Server (via SignalR) and WebAssembly (direct)
            if (!IsPrerendering)
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
                catch (InvalidOperationException ex)
                {
                    // WHY: Expected during prerendering or if JS not available
                    // HOW: Already stored in session/cookies, so this is non-critical
                    _logger.LogDebug(ex, "Could not store in localStorage");
                }
            }
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
            var context = _httpContextAccessor.HttpContext;
            if (context != null)
            {
                // Clear session
                if (!context.Response.HasStarted)
                {
                    context.Session.Remove("auth_token");
                    context.Session.Remove("refresh_token");
                }

                // Clear cookies
                context.Response.Cookies.Delete("auth_token");
                context.Response.Cookies.Delete("refresh_token");
            }

            // Clear localStorage
            if (!IsPrerendering && _jsRuntime is IJSInProcessRuntime)
            {
                try
                {
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "auth_token");
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "refresh_token");
                }
                catch (InvalidOperationException)
                {
                    // Expected during prerendering
                }
            }

            _logger.LogInformation("Authentication tokens cleared from all storage");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing authentication tokens");
        }
    }
}
