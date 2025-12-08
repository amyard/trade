namespace MarketParse.Models;

/// <summary>
/// Configuration model for trading pairs
/// </summary>
public class TradingPairsConfig
{
    public List<TradingPairInfo> TradingPairs { get; set; } = new();
}

/// <summary>
/// Information about a trading pair
/// </summary>
public class TradingPairInfo
{
    public string Symbol { get; set; } = string.Empty;
    public Priority Priority { get; set; } = Priority.None;
}
