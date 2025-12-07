namespace MarketParse.Models;

/// <summary>
/// Trading pair model
/// </summary>
public class TradingPair
{
    public string Symbol { get; set; } = string.Empty;
    public string BaseAsset { get; set; } = string.Empty;
    public string QuoteAsset { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    
    public TradingPair(string symbol, string baseAsset, string quoteAsset)
    {
        Symbol = symbol;
        BaseAsset = baseAsset;
        QuoteAsset = quoteAsset;
        DisplayName = $"{baseAsset}/{quoteAsset}";
    }
}
