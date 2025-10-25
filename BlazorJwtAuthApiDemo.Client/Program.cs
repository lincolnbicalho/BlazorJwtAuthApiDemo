using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using BlazorJwtAuthApiDemo.Client.Services;
using BlazorJwtAuthApiDemo.Shared.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// STEP 1: Configure HttpClient with API base address and JWT authorization
// WHY: WebAssembly components need to communicate with the API using JWT authentication
// HOW: Use ApiAuthorizationMessageHandler to automatically inject JWT token
// NOTE: Hardcoded API address for reliability in hybrid hosting mode
var apiBaseAddress = builder.Configuration["ApiBaseAddress"] ?? "https://localhost:7230";

builder.Services.AddScoped<ApiAuthorizationMessageHandler>();

builder.Services.AddScoped(sp =>
{
    var handler = sp.GetRequiredService<ApiAuthorizationMessageHandler>();
    handler.InnerHandler = new HttpClientHandler();

    var httpClient = new HttpClient(handler)
    {
        BaseAddress = new Uri(apiBaseAddress)
    };

    return httpClient;
});

// STEP 2: Add authentication services for WebAssembly
// WHY: ClientAuthTokenService uses localStorage (the only option in WASM)
builder.Services.AddScoped<IAuthTokenService, ClientAuthTokenService>();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(provider =>
    provider.GetRequiredService<CustomAuthStateProvider>());

// STEP 3: Add authorization core
builder.Services.AddAuthorizationCore();

// STEP 4: Register ApiService for consistent API access pattern
builder.Services.AddScoped<IApiService, ApiService>();

await builder.Build().RunAsync();
