using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using BlazorJwtAuthApiDemo.Components;
using BlazorJwtAuthApiDemo.Services;
using BlazorJwtAuthApiDemo.Shared.Services;

var builder = WebApplication.CreateBuilder(args);

// ===== Add services to the container =====

// Add Razor Components with Interactive Server and WebAssembly modes
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// ===== Configure Session for HybridAuthTokenService =====
// WHY: Session storage is the fastest option for token retrieval
// HOW: Session stores data in-memory on the server
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(1);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// ===== Configure Cookie Authentication =====
// WHY: Web app uses cookie authentication, JWT is stored as a claim
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/login";
    options.AccessDeniedPath = "/login";
    options.ExpireTimeSpan = TimeSpan.FromDays(1);
    options.SlidingExpiration = true;
});

// Add authorization services
builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// ===== Register application services =====

// WHY: HttpContextAccessor required by HybridAuthTokenService
builder.Services.AddHttpContextAccessor();

// WHY: HybridAuthTokenService implements multi-layered token storage
// HOW: Session -> Cookie -> localStorage (based on context)
builder.Services.AddScoped<IAuthTokenService, HybridAuthTokenService>();

// WHY: CustomAuthStateProvider for JWT-based authentication state
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

// WHY: HttpClient for API communication
builder.Services.AddHttpClient<IApiService, ApiService>();

// WHY: Scoped services for per-request/circuit lifetime
builder.Services.AddScoped<IApiService, ApiService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// NOTE: HttpClient for WASM components is configured in Client/Program.cs
// DO NOT register a generic HttpClient here as it will override the Client's configuration

var app = builder.Build();

// ===== Configure the HTTP request pipeline =====

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
else
{
    app.UseWebAssemblyDebugging();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// WHY: Session must be before authentication
app.UseSession();

// WHY: Authentication must come before authorization
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// Map Razor Components
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(BlazorJwtAuthApiDemo.Client._Imports).Assembly);

app.Run();
