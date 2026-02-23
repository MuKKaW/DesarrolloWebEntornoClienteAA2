namespace Subastas.Api.Models;

public class Product
{
    public long Id { get; set; }
    public long CreatedBy { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public decimal StartPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public string Status { get; set; } = "DRAFT";

    public User? CreatedByUser { get; set; }

    public DateTime CreatedAt { get; set; }
    public List<Bid> Bids { get; set; } = new();
}