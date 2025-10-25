namespace BlazorJwtAuthApiDemo.Shared.Models;

/// <summary>
/// User model without sensitive information (password hash)
/// </summary>
public class UserModel
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}
