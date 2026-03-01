namespace Subastas.Api.Models;

public class User
{
    public long Id { get; set; }
    public string Email { get; set; } = "";
    public string Nickname { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "USER";
    public bool IsAdmin { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
