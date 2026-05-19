namespace B2BBuyback.Api.DTOs;

public class LoginRequest
{
    // Can be either username or email
    public string Identifier { get; set; } = null!;
    
    public string Password { get; set; } = null!;
}