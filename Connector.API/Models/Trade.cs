namespace Connector.API.Models;

public class Trade
{
    public required string Id { get; set; }
    public required string Pair { get; set; }
    public decimal Price { get; set; }
    public decimal Amount { get; set; }
    public required string Side { get; set; }
    public DateTimeOffset Time { get; set; }
}
