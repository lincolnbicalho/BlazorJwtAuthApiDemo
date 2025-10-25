# Blazor 8 JWT Authentication Demo

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/8.0)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Blazor](https://img.shields.io/badge/Blazor-8.0-purple)](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)

**Production-ready Blazor 8 JWT authentication demo with separate Web API**

A complete, working implementation of JWT authentication in Blazor 8 using InteractiveAuto rendermode with a separate ASP.NET Core Web API. This project demonstrates the authentication patterns described in the blog series on [ljblab.dev](https://ljblab.dev).

## Overview

This demo project showcases a **production-ready authentication architecture** for Blazor 8 applications, addressing the unique challenges of hybrid rendering modes (SSR, Server, WebAssembly, and Auto).

### Key Features

- ✅ **Blazor 8** with InteractiveAuto rendermode support
- ✅ **Separate ASP.NET Core Web API** for authentication
- ✅ **JWT token-based authentication** with refresh token support
- ✅ **Hybrid storage pattern** (Cookie + JWT + localStorage)
- ✅ **TokenBridgeComponent** for automatic token synchronization
- ✅ **No database required** - uses mock user repository for demo
- ✅ **Production-ready architecture** based on enterprise patterns
- ✅ **All render modes supported** (SSR, Server, WASM, Auto)
- ✅ **Cross-platform** compatible (Windows, macOS, Linux)

## Architecture

This project uses a three-tier architecture:

```
┌─────────────────────────────────────────────────────────────┐
│                   Blazor Application                        │
│  ┌──────────────────────────────────────────────────┐      │
│  │  BlazorJwtAuthApiDemo (Server)                    │      │
│  │  - HybridAuthTokenService (Session/Cookie/LS)    │      │
│  │  - TokenBridgeComponent (Auto sync)              │      │
│  │  - Login/Register/Logout pages                   │      │
│  └──────────────────────────────────────────────────┘      │
│  ┌──────────────────────────────────────────────────┐      │
│  │  BlazorJwtAuthApiDemo.Client (WASM)               │      │
│  │  - ClientAuthTokenService (localStorage only)    │      │
│  │  - Client-side components                        │      │
│  └──────────────────────────────────────────────────┘      │
│  ┌──────────────────────────────────────────────────┐      │
│  │  BlazorJwtAuthApiDemo.Shared                      │      │
│  │  - TokenBridgeComponent.razor                    │      │
│  │  - IAuthTokenService interface                   │      │
│  │  - Shared models                                 │      │
│  └──────────────────────────────────────────────────┘      │
└─────────────────────────────────────────────────────────────┘
                             ↕ HTTP/HTTPS (JWT Bearer)
┌─────────────────────────────────────────────────────────────┐
│              BlazorJwtAuthApiDemo.Api                        │
│  - AuthController (Login/Register/Refresh)                  │
│  - JwtTokenService (JWT generation/validation)             │
│  - MockUserRepository (Demo user data)                     │
│  - WeatherForecastController (Protected endpoint)          │
└─────────────────────────────────────────────────────────────┘
```

### Authentication Flow

1. **User Login** → User submits credentials via Login.razor
2. **API Validation** → API validates credentials and generates JWT + refresh token
3. **Server Storage** → Token stored in server-side claims and secure cookies
4. **Token Bridge** → TokenBridgeComponent transfers token to localStorage on client render
5. **API Calls** → Subsequent requests use token from appropriate storage (session/cookie/localStorage)

### Token Storage Strategy

The hybrid storage approach ensures tokens are available regardless of render mode:

| Render Mode | Primary Storage | Fallback | Token Bridge |
|-------------|----------------|----------|--------------|
| SSR | Session | Cookie | Not needed |
| Server | Session + Cookie | N/A | Transfers to localStorage |
| WASM | localStorage | Cookie | Not needed (client-side login) |
| Auto | All layers | Session → Cookie → localStorage | **Critical** for sync |

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later
- IDE: Visual Studio 2022 (17.8+), VS Code, or JetBrains Rider
- Basic understanding of Blazor and JWT authentication concepts

## Getting Started

### Installation

```bash
git clone https://github.com/lincolnbicalho/BlazorJwtAuthApiDemo.git
cd BlazorJwtAuthApiDemo
```

### Running the Application

#### Option 1: Visual Studio

1. Open `BlazorJwtAuthApiDemo.sln`
2. Right-click solution → **Set Startup Projects**
3. Select **Multiple startup projects**
4. Set both `BlazorJwtAuthApiDemo.Api` and `BlazorJwtAuthApiDemo` to **Start**
5. Press **F5** to run

#### Option 2: Command Line

Open two terminal windows:

**Terminal 1 - Start the API:**
```bash
cd BlazorJwtAuthApiDemo.Api
dotnet run
```

**Terminal 2 - Start the Blazor App:**
```bash
cd BlazorJwtAuthApiDemo
dotnet run
```

#### Option 3: VS Code

1. Open the project folder
2. Use the included launch configuration (if available)
3. Or manually start both projects in separate terminals as shown in Option 2

### Default URLs

- **Blazor App**: `https://localhost:7001`
- **API**: `https://localhost:7002`

> 📝 **Note**: If ports are already in use, check `Properties/launchSettings.json` in each project to see which ports are configured, or the application will auto-assign available ports.

### Test Credentials

Use these pre-configured accounts to test the authentication:

```
Email: demo@example.com
Password: Demo123!

Email: admin@example.com
Password: Admin123!
```

These accounts are defined in `MockUserRepository.cs` and are for demonstration purposes only.

## Project Structure

```
BlazorJwtAuthApiDemo/
├── BlazorJwtAuthApiDemo/                  # Main Blazor Server application
│   ├── Components/
│   │   ├── Layout/
│   │   │   └── MainLayout.razor           # Includes TokenBridgeComponent
│   │   └── Pages/
│   │       ├── Home.razor                 # Landing page
│   │       ├── Login.razor                # User login
│   │       ├── Register.razor             # User registration
│   │       ├── Logout.razor               # User logout
│   │       ├── Weather.razor              # Protected WASM example
│   │       └── WeatherServer.razor        # Protected Server example
│   ├── Services/
│   │   ├── HybridAuthTokenService.cs      # Multi-layer token storage
│   │   ├── AuthService.cs                 # Authentication business logic
│   │   ├── ApiService.cs                  # API communication helper
│   │   └── CustomAuthStateProvider.cs    # Blazor auth state management
│   └── Program.cs                         # Server app configuration
│
├── BlazorJwtAuthApiDemo.Client/           # WebAssembly client components
│   ├── Services/
│   │   ├── ClientAuthTokenService.cs      # Client-side token storage (localStorage)
│   │   └── ApiAuthorizationMessageHandler.cs  # HTTP client auth handler
│   └── Program.cs                         # Client configuration
│
├── BlazorJwtAuthApiDemo.Api/              # Separate Web API
│   ├── Controllers/
│   │   ├── AuthController.cs              # Authentication endpoints
│   │   │   - POST /api/auth/login
│   │   │   - POST /api/auth/register
│   │   │   - POST /api/auth/refresh
│   │   └── WeatherForecastController.cs   # Protected API example
│   │       - GET /api/weatherforecast
│   ├── Services/
│   │   ├── JwtTokenService.cs             # JWT generation and validation
│   │   └── MockUserRepository.cs          # In-memory user data
│   └── Program.cs                         # API configuration
│
└── BlazorJwtAuthApiDemo.Shared/           # Shared code across projects
    ├── Components/
    │   └── TokenBridgeComponent.razor     # Token synchronization component
    ├── Models/
    │   ├── LoginRequest.cs
    │   ├── RegisterRequest.cs
    │   ├── AuthResponse.cs
    │   └── User.cs
    └── Services/
        └── IAuthTokenService.cs           # Token service interface
```

## Key Components Explained

### TokenBridgeComponent

**The critical missing piece** for Blazor 8 hybrid authentication.

```razor
@rendermode InteractiveAuto

@code {
    // Automatically transfers JWT from server-side claims to localStorage
    // when JavaScript becomes available
}
```

**Why it's needed:**
- Server-side login can't access localStorage (JavaScript not available during request)
- Client-side components (especially WASM) need tokens in localStorage
- The bridge runs once after first render and transfers the token automatically

**How it works:**
1. Uses `@rendermode InteractiveAuto` to run client-side
2. Executes `OnAfterRenderAsync` when JavaScript is available
3. Extracts JWT from `ClaimsPrincipal` (stored during server-side login)
4. Stores token in localStorage for client components
5. Logs success/failure for debugging

**Location:** `BlazorJwtAuthApiDemo.Shared/Components/TokenBridgeComponent.razor`
**Usage:** Added to `MainLayout.razor` to run on every page

### HybridAuthTokenService

Multi-layer storage service that tries multiple storage mechanisms in sequence:

1. **Session** (fastest, server-side only)
2. **Cookie** (persistent, works everywhere)
3. **localStorage** (client-side, for WASM components)

**Key features:**
- Prerendering-safe (checks `Response.HasStarted`)
- Automatic fallback mechanism
- Works in all Blazor 8 render modes
- Caches tokens in memory for performance

### Authentication Flow Details

**Login Process:**
1. User enters credentials in `Login.razor`
2. `AuthService.LoginAsync()` calls API `/api/auth/login`
3. API validates credentials via `MockUserRepository`
4. `JwtTokenService` generates access token (15 min) + refresh token (7 days)
5. Server stores JWT as "jwt" claim in `ClaimsPrincipal`
6. `HybridAuthTokenService` stores token in session and cookie
7. `TokenBridgeComponent` transfers token to localStorage on client render
8. User redirected to home page

**Token Usage:**
- Server components: Read from session/cookie via `HybridAuthTokenService`
- WASM components: Read from localStorage via `ClientAuthTokenService`
- API calls: Token sent in `Authorization: Bearer {token}` header

**Token Refresh:**
- When access token expires (15 min), refresh token used to get new access token
- Refresh tokens valid for 7 days
- Automatic refresh happens before token expiry to prevent auth failures

## API Endpoints

### Authentication Endpoints

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "demo@example.com",
  "password": "Demo123!"
}

Response:
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "refresh_token_here",
  "expiresIn": 900
}
```

```http
POST /api/auth/register
Content-Type: application/json

{
  "email": "newuser@example.com",
  "password": "NewPass123!",
  "fullName": "New User"
}

Response:
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "refreshToken": "refresh_token_here",
  "expiresIn": 900
}
```

```http
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "refresh_token_here"
}

Response:
{
  "accessToken": "new_access_token_here",
  "refreshToken": "new_refresh_token_here",
  "expiresIn": 900
}
```

### Protected Endpoint Example

```http
GET /api/weatherforecast
Authorization: Bearer eyJhbGciOiJIUzI1NiIs...

Response:
[
  {
    "date": "2024-10-25",
    "temperatureC": 15,
    "summary": "Cool"
  }
]
```

## Configuration

### API Configuration (appsettings.json)

```json
{
  "Jwt": {
    "Key": "your-super-secret-key-at-least-32-characters-long-for-production",
    "Issuer": "BlazorJwtAuthApiDemo",
    "Audience": "BlazorJwtAuthApiDemo",
    "ExpiresInMinutes": 15,
    "RefreshTokenExpiresInDays": 7
  },
  "Cors": {
    "AllowedOrigins": [
      "https://localhost:7001"
    ]
  }
}
```

> ⚠️ **Security Warning**: The default JWT secret key is for **demo purposes only**. In production:
> - Generate a strong random key (minimum 32 characters)
> - Store it in Azure Key Vault or environment variables
> - Never commit secrets to source control
> - Use different keys for different environments

### Blazor App Configuration (appsettings.json)

```json
{
  "ApiBaseUrl": "https://localhost:7002"
}
```

## Troubleshooting

### Issue: "JavaScript interop calls cannot be issued at this time"

**Symptoms:**
- Error during server-side rendering
- Application crashes on prerender

**Cause:**
Attempting to call JavaScript during server-side prerendering when no JS runtime is available.

**Solution:**
The `TokenBridgeComponent` handles this automatically by:
- Using `OnAfterRenderAsync` instead of `OnInitializedAsync`
- Checking `IsPrerendering` before JS calls
- Verify `TokenBridgeComponent` is added to `MainLayout.razor`

### Issue: Token not found in localStorage

**Symptoms:**
- Login succeeds but client components can't make authenticated API calls
- Browser DevTools shows no "auth_token" in localStorage
- 401 Unauthorized errors from API

**Cause:**
`TokenBridge` not transferring token from server-side claims to localStorage.

**Solution:**
1. Verify `TokenBridgeComponent` is in `MainLayout.razor`
2. Check that login process stores JWT as "jwt" claim in `ClaimsPrincipal`
3. Inspect browser console for "Token bridge" log messages
4. Clear browser cache and cookies, then try again

### Issue: API returns 401 Unauthorized

**Symptoms:**
- All API calls fail with 401 status
- Even after successful login

**Cause:**
- Token not being sent with requests
- Token expired
- Invalid token format

**Solution:**
1. Open browser DevTools → Network tab
2. Find a failed API request
3. Check Request Headers → Look for `Authorization: Bearer {token}`
4. If missing: Token service not working correctly
5. If present: Token may be expired or invalid
6. Verify JWT secret key matches between API and token generation

### Issue: CORS errors when calling API

**Symptoms:**
```
Access to fetch at 'https://localhost:7002/api/auth/login' from origin
'https://localhost:7001' has been blocked by CORS policy
```

**Cause:**
API CORS policy doesn't allow requests from Blazor app origin.

**Solution:**
1. Check `Program.cs` in API project
2. Verify CORS configuration includes your Blazor app URL:
```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("https://localhost:7001")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
```
3. Ensure `app.UseCors()` is called before `app.MapControllers()`

### Issue: Login works but logout doesn't clear token

**Symptoms:**
- User clicks logout but can still access protected pages
- Token still visible in browser storage

**Cause:**
`ClearTokensAsync()` not being called properly.

**Solution:**
1. Verify `Logout.razor` calls `AuthService.LogoutAsync()`
2. Check that `ClearTokensAsync()` clears all storage layers:
   - Session (server-side)
   - Cookies
   - localStorage (client-side)
3. Ensure `NotifyAuthenticationStateChanged()` is called after logout

### Issue: Session lost after server restart

**Symptoms:**
- Users logged out after restarting the server
- Development workflow interrupted

**Cause:**
In-memory session storage doesn't persist across restarts.

**Solution (Development):**
This is expected behavior in development. Users need to re-login after server restart.

**Solution (Production):**
Use distributed cache (Redis) for session persistence:
```csharp
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "your-redis-connection-string";
});
```

## Roadmap

### Current Version (v1.0)

- ✅ JWT authentication with separate Web API
- ✅ Hybrid token storage (Session/Cookie/localStorage)
- ✅ TokenBridge component for auto-sync
- ✅ Login, Register, Logout pages
- ✅ Protected route examples (Server and WASM)
- ✅ Mock user repository (no database)
- ✅ Refresh token support
- ✅ All Blazor 8 render modes supported

### Planned Features (v2.0)

- [ ] **Database Integration**: Entity Framework Core with SQL Server/PostgreSQL
- [ ] **Email Verification**: Send confirmation emails on registration
- [ ] **Password Reset**: Forgot password flow with email token
- [ ] **Role-Based Authorization**: Demonstrate role/claim-based access control
- [ ] **Two-Factor Authentication (2FA)**: TOTP-based 2FA support
- [ ] **Remember Me**: Persistent login with extended refresh tokens
- [ ] **Account Lockout**: Security feature after failed login attempts
- [ ] **Audit Logging**: Track authentication events
- [ ] **Docker Support**: Containerization with docker-compose
- [ ] **CI/CD Examples**: GitHub Actions workflow
- [ ] **Integration Tests**: Automated testing for auth flows
- [ ] **API Documentation**: Swagger/OpenAPI integration

### Future Considerations

- OAuth 2.0 / OpenID Connect integration
- Azure AD / Entra ID authentication
- Multi-tenant support
- GraphQL API alternative
- Blazor MAUI integration example

## Related Resources

### Blog Series

This project implements the patterns described in detail on my blog:

1. **[Solving Blazor 8's Authentication Crisis](https://ljblab.dev/blog/solving-blazor-8-authentication-crisis)**
   *How a week of frustration led to a production-ready solution*

2. **[JWT Authentication in Blazor 8: Production Implementation Guide](https://ljblab.dev/blog/blazor-jwt-authentication-deep-dive)**
   *Complete reference guide with production-ready code*

3. **[Securing Blazor Applications for Enterprise Production Environments](https://ljblab.dev/blog/blazor-production-security-enterprise)**
   *Security best practices and comprehensive monitoring*

### Official Documentation

- [Blazor Authentication and Authorization](https://learn.microsoft.com/aspnet/core/blazor/security/) - Microsoft Docs
- [JWT Bearer Authentication](https://learn.microsoft.com/aspnet/core/security/authentication/jwt-authn) - Microsoft Docs
- [Blazor Render Modes](https://learn.microsoft.com/aspnet/core/blazor/components/render-modes) - Microsoft Docs

### Community Resources

- [Awesome Blazor](https://github.com/AdrienTorris/awesome-blazor) - Curated list of Blazor resources
- [Blazor University](https://blazor-university.com/) - Free Blazor tutorials

## Contributing

Contributions are welcome! This project is intended as a learning resource and demo, so improvements and suggestions are appreciated.

**Ways to contribute:**
- 🐛 Report bugs by opening an issue
- 💡 Suggest new features or improvements
- 📖 Improve documentation
- 🔧 Submit pull requests

**Before contributing:**
1. Check existing issues and pull requests
2. For major changes, open an issue first to discuss
3. Follow the existing code style and patterns
4. Add comments explaining complex logic
5. Test your changes thoroughly

## License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

**MIT License Summary:**
- ✅ Commercial use allowed
- ✅ Modification allowed
- ✅ Distribution allowed
- ✅ Private use allowed
- ℹ️ License and copyright notice required
- ⚠️ Provided "as is" without warranty

## Author & Support

**Lincoln J Bicalho**
- 🌐 Website: [ljblab.dev](https://ljblab.dev)
- 📧 Email: lincoln@ljblab.dev
- 💼 LinkedIn: [Lincoln J Bicalho](https://www.linkedin.com/in/lincolnbicalho)
- 📝 Blog: Technical articles on Blazor, AI/ML, and enterprise development

**Professional Services:**
- Architecture review and consulting
- Enterprise authentication implementations
- Blazor training and mentoring
- Security audits and compliance

---

## Important Security Notes

⚠️ **This is a demo/learning project**. While based on production patterns, additional security measures are required for production use:

### Production Checklist

- [ ] **Secure Key Management**: Store JWT secrets in Azure Key Vault or similar
- [ ] **HTTPS Enforcement**: Force HTTPS in production environments
- [ ] **Rate Limiting**: Implement rate limiting on authentication endpoints
- [ ] **Database Security**: Use real database with encryption at rest
- [ ] **Security Headers**: Configure CSP, HSTS, X-Frame-Options, etc.
- [ ] **Input Validation**: Comprehensive validation and sanitization
- [ ] **CSRF Protection**: Enable anti-forgery tokens
- [ ] **Logging & Monitoring**: Application Insights or similar
- [ ] **Password Policy**: Enforce strong password requirements
- [ ] **Account Lockout**: Implement after N failed attempts
- [ ] **Email Verification**: Verify email addresses on registration
- [ ] **Security Scanning**: Regular vulnerability scans
- [ ] **Penetration Testing**: Before production deployment

### Known Limitations (Demo Only)

- Uses in-memory mock user repository (no persistence)
- Simple password hashing (use stronger algorithms in production)
- No rate limiting or account lockout
- JWT secret in configuration (should be in secure vault)
- No email verification or password reset
- Simplified error handling for clarity
- No comprehensive logging/monitoring

---

**⭐ If you find this project helpful, please consider starring the repository!**

**📚 Check out the [blog series](https://ljblab.dev/blog) for in-depth explanations of the authentication patterns used here.**
