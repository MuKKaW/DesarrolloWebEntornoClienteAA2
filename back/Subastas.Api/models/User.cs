namespace Subastas.Api.Models;

public class User
{
    public long Id { get; set; }
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "USER";
    public DateTime CreatedAt { get; set; }
}