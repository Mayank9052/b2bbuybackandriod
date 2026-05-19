namespace B2BBuyback.Api.DTOs;

public class RegisterRequest
{
    public string Username { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string Password { get; set; } = null!;

    public string Role { get; set; } = "User";
    public string? PhoneNumber { get; set; }
    public string? Department { get; set; }
}