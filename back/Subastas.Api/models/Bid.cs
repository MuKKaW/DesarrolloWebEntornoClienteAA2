namespace Subastas.Api.Models;

public class Bid
{
    public long Id { get; set; }
    public long ProductId { get; set; }
    public long UserId { get; set; }
    public Product? Product { get; set; }

    public User? User { get; set; }

    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; }
}