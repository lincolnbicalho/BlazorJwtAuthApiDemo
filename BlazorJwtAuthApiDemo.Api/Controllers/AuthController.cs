using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BlazorJwtAuthApiDemo.Api.Models;
using BlazorJwtAuthApiDemo.Api.Services;
using BlazorJwtAuthApiDemo.Api.Data;

namespace BlazorJwtAuthApiDemo.Api.Controllers;

/// <summary>
/// Authentication controller for login and registration
/// Based on production implementation from Bliik.Therapists
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ITokenService _tokenService;
    private readonly MockUserRepository _userRepository;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;

    public AuthController(
        ITokenService tokenService,
        MockUserRepository userRepository,
        ILogger<AuthController> logger,
        IConfiguration configuration)
    {
        _tokenService = tokenService;
        _userRepository = userRepository;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Test endpoint to verify API is running
    /// </summary>
    [HttpGet("test")]
    [AllowAnonymous]
    public ActionResult<object> Test()
    {
        return Ok(new
        {
            message = "Blazor JWT Auth API is running!",
            timestamp = DateTime.UtcNow,
            status = "success"
        });
    }

    /// <summary>
    /// Authenticates a user and returns a JWT token
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>JWT token and user information</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            _logger.LogInformation("Login attempt for email: {Email}", request.Email);

            // STEP 1: Validate input
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid input data" });
            }

            // STEP 2: Find user by email
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("Login failed: User not found for email {Email}", request.Email);
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // STEP 3: Verify password
            // WHY: Use BCrypt to verify the hashed password
            // HOW: BCrypt.Verify compares the plain-text password with the stored hash
            if (string.IsNullOrEmpty(user.PasswordHash) ||
                !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed: Invalid password for email {Email}", request.Email);
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // STEP 4: Check if user is active
            if (!user.IsActive)
            {
                _logger.LogWarning("Login failed: Inactive user {Email}", request.Email);
                return Unauthorized(new { message = "Your account has been deactivated" });
            }

            // STEP 5: Generate JWT tokens
            var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email, user.Roles);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // STEP 6: Update last login time
            await _userRepository.UpdateLastLoginAsync(user.Id);

            // STEP 7: Create response
            var expireMinutes = int.Parse(_configuration["JWT:ExpireMinutes"] ?? "60");

            var response = new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Roles = user.Roles
                },
                ExpiresAt = DateTime.UtcNow.AddMinutes(expireMinutes)
            };

            _logger.LogInformation("Login successful for user {Email}", request.Email);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    /// <summary>
    /// Registers a new user
    /// </summary>
    /// <param name="request">Registration information</param>
    /// <returns>JWT token and user information</returns>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            _logger.LogInformation("Registration attempt for email: {Email}", request.Email);

            // STEP 1: Validate input
            if (!ModelState.IsValid)
            {
                return BadRequest(new { message = "Invalid input data", errors = ModelState });
            }

            // STEP 2: Check if email already exists
            if (await _userRepository.EmailExistsAsync(request.Email))
            {
                _logger.LogWarning("Registration failed: Email already exists {Email}", request.Email);
                return BadRequest(new { message = "Email address is already registered" });
            }

            // STEP 3: Create new user
            // WHY: Hash password using BCrypt for security
            // HOW: BCrypt.HashPassword creates a secure hash that includes a salt
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Roles = new List<string> { "User" }, // Default role
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            // STEP 4: Save user to repository
            await _userRepository.AddAsync(user);

            // STEP 5: Generate JWT tokens
            var accessToken = _tokenService.GenerateAccessToken(user.Id, user.Email, user.Roles);
            var refreshToken = _tokenService.GenerateRefreshToken();

            // STEP 6: Create response
            var expireMinutes = int.Parse(_configuration["JWT:ExpireMinutes"] ?? "60");

            var response = new LoginResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Roles = user.Roles
                },
                ExpiresAt = DateTime.UtcNow.AddMinutes(expireMinutes)
            };

            _logger.LogInformation("Registration successful for user {Email}", request.Email);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for email {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred during registration" });
        }
    }

    /// <summary>
    /// Protected endpoint to test JWT authentication
    /// Requires valid JWT token in Authorization header
    /// </summary>
    [HttpGet("me")]
    [Authorize] // This requires a valid JWT token
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        try
        {
            // WHY: Extract user ID from JWT token claims
            // HOW: The [Authorize] attribute validates the JWT and populates User.Claims
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid token" });
            }

            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = user.Roles
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }
}
