namespace Connector.API.Models;

public class Candle
{
    public required string Pair { get; set; }
    public decimal OpenPrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal ClosePrice { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal TotalVolume { get; set; }
    public DateTimeOffset OpenTime { get; set; }
}
