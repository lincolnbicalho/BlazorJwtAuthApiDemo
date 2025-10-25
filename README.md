# Blazor 8 JWT Authentication Demo



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

Open two terminal windows and start both projects:

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

**Default URLs:**
- Blazor App: `https://localhost:7001`
- API: `https://localhost:7002`

> 📝 **Note**: If ports are in use, check `Properties/launchSettings.json` in each project.

### Test Credentials

```
Email: demo@example.com
Password: Demo123!

Email: admin@example.com
Password: Admin123!
```

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

## Configuration

Update `appsettings.json` files if you need to change ports or settings.

**API Configuration:** `BlazorJwtAuthApiDemo.Api/appsettings.json` - Configure JWT settings and CORS allowed origins

**Blazor App Configuration:** `BlazorJwtAuthApiDemo/appsettings.json` - Set API base URL

> ⚠️ **Security Warning**: The default JWT secret key is for **demo purposes only**. In production, store secrets in Azure Key Vault or environment variables.

## Common Issues

### Token not found in localStorage

**Symptoms:** Login succeeds but client components can't make authenticated API calls

**Solution:**
1. Verify `TokenBridgeComponent` is in `MainLayout.razor`
2. Check that login process stores JWT as "jwt" claim in `ClaimsPrincipal`
3. Inspect browser console for "Token bridge" log messages
4. Clear browser cache and cookies, then try again

### CORS errors when calling API

**Symptoms:** Browser blocks API requests with CORS policy error

**Solution:**
1. Verify CORS configuration in API `Program.cs` includes your Blazor app URL (`https://localhost:7001`)
2. Ensure `app.UseCors()` is called before `app.MapControllers()`

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

## License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

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

⚠️ **This is a demo/learning project**. While based on production patterns, additional security measures are required for production use (secure key management, rate limiting, database security, email verification, etc.).

⭐ **If you find this project helpful, please consider starring the repository!**
