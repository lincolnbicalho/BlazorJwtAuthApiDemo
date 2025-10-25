using BlazorJwtAuthApiDemo.Api.Models;

namespace BlazorJwtAuthApiDemo.Api.Data;

/// <summary>
/// In-memory user repository for demo purposes
/// In production, this would be replaced with a database repository
/// </summary>
public class MockUserRepository
{
    private readonly List<User> _users = new();

    public MockUserRepository()
    {
        // WHY: Seed initial test users for demo purposes
        // HOW: Using BCrypt to hash passwords (same as production)
        SeedUsers();
    }

    /// <summary>
    /// Seeds the repository with test users
    /// Passwords are hashed using BCrypt
    /// </summary>
    private void SeedUsers()
    {
        _users.Add(new User
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Email = "admin@demo.com",
            FirstName = "Admin",
            LastName = "User",
            // Password: Admin123!
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            Roles = new List<string> { "Admin", "User" },
            CreatedAt = DateTime.UtcNow.AddMonths(-6),
            IsActive = true
        });

        _users.Add(new User
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Email = "therapist@demo.com",
            FirstName = "Sarah",
            LastName = "Johnson",
            // Password: Therapist123!
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Therapist123!"),
            Roles = new List<string> { "Therapist", "User" },
            CreatedAt = DateTime.UtcNow.AddMonths(-3),
            IsActive = true
        });

        _users.Add(new User
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Email = "user@demo.com",
            FirstName = "John",
            LastName = "Doe",
            // Password: User123!
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("User123!"),
            Roles = new List<string> { "User" },
            CreatedAt = DateTime.UtcNow.AddMonths(-1),
            IsActive = true
        });
    }

    /// <summary>
    /// Finds a user by email address
    /// </summary>
    /// <param name="email">Email address to search for</param>
    /// <returns>User if found, null otherwise</returns>
    public async Task<User?> GetByEmailAsync(string email)
    {
        // WHY: Simulate async database call with Task.FromResult
        // HOW: Case-insensitive email comparison
        return await Task.FromResult(
            _users.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
        );
    }

    /// <summary>
    /// Finds a user by ID
    /// </summary>
    /// <param name="id">User ID to search for</param>
    /// <returns>User if found, null otherwise</returns>
    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await Task.FromResult(_users.FirstOrDefault(u => u.Id == id));
    }

    /// <summary>
    /// Checks if a user with the given email already exists
    /// </summary>
    /// <param name="email">Email to check</param>
    /// <returns>True if exists, false otherwise</returns>
    public async Task<bool> EmailExistsAsync(string email)
    {
        return await Task.FromResult(
            _users.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
        );
    }

    /// <summary>
    /// Adds a new user to the repository
    /// </summary>
    /// <param name="user">User to add</param>
    /// <returns>Created user</returns>
    public async Task<User> AddAsync(User user)
    {
        // WHY: Ensure unique ID and set creation date
        if (user.Id == Guid.Empty)
            user.Id = Guid.NewGuid();

        user.CreatedAt = DateTime.UtcNow;

        _users.Add(user);

        return await Task.FromResult(user);
    }

    /// <summary>
    /// Updates the last login time for a user
    /// </summary>
    /// <param name="userId">ID of the user who logged in</param>
    public async Task UpdateLastLoginAsync(Guid userId)
    {
        var user = _users.FirstOrDefault(u => u.Id == userId);
        if (user != null)
        {
            user.LastLoginAt = DateTime.UtcNow;
        }
        await Task.CompletedTask;
    }

    /// <summary>
    /// Gets all users (for admin purposes - not exposed via API in this demo)
    /// </summary>
    /// <returns>List of all users</returns>
    public async Task<List<User>> GetAllAsync()
    {
        return await Task.FromResult(_users.ToList());
    }
}
