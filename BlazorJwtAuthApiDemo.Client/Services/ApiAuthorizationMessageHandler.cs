using BlazorJwtAuthApiDemo.Shared.Services;
using System.Net.Http.Headers;

namespace BlazorJwtAuthApiDemo.Client.Services;

/// <summary>
/// Delegating handler that adds JWT token to outgoing HTTP requests in WebAssembly
/// Based on the pattern from "Solving Blazor 8's Authentication Crisis"
/// https://ljblab.dev/blog/solving-blazor-8-authentication-crisis
/// </summary>
public class ApiAuthorizationMessageHandler : DelegatingHandler
{
    private readonly IAuthTokenService _tokenService;
    private readonly ILogger<ApiAuthorizationMessageHandler> _logger;

    public ApiAuthorizationMessageHandler(
        IAuthTokenService tokenService,
        ILogger<ApiAuthorizationMessageHandler> logger)
    {
        _tokenService = tokenService;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        try
        {
            // WHY: Get JWT token from localStorage via ClientAuthTokenService
            // HOW: Token is stored during login by HybridAuthTokenService
            var token = await _tokenService.GetTokenAsync();

            if (!string.IsNullOrEmpty(token))
            {
                // WHY: Add Bearer token to Authorization header for API authentication
                // HOW: API validates this JWT using TokenService.ValidateToken
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                _logger.LogDebug("Authorization header added to request: {Method} {Uri}",
                    request.Method, request.RequestUri);
            }
            else
            {
                _logger.LogWarning("No authentication token available for request: {Method} {Uri}",
                    request.Method, request.RequestUri);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding authorization header to request");
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
