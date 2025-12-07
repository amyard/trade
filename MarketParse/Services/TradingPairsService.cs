using MarketParse.Models;

namespace MarketParse.Services;

/// <summary>
/// Service for managing trading pairs list
/// </summary>
public class TradingPairsService
{
    private static readonly List<TradingPair> _tradingPairs = new()
    {
        new TradingPair("BTCUSDT", "BTC", "USDT"),
        new TradingPair("ETHUSDT", "ETH", "USDT"),
        new TradingPair("SOLUSDT", "SOL", "USDT"),
        new TradingPair("BNBUSDT", "BNB", "USDT"),
        new TradingPair("XRPUSDT", "XRP", "USDT"),
        new TradingPair("ADAUSDT", "ADA", "USDT"),
        new TradingPair("DOGEUSDT", "DOGE", "USDT"),
        new TradingPair("MATICUSDT", "MATIC", "USDT"),
        new TradingPair("DOTUSDT", "DOT", "USDT"),
        new TradingPair("LTCUSDT", "LTC", "USDT"),
        new TradingPair("AVAXUSDT", "AVAX", "USDT"),
        new TradingPair("LINKUSDT", "LINK", "USDT"),
        new TradingPair("ATOMUSDT", "ATOM", "USDT"),
        new TradingPair("ETCUSDT", "ETC", "USDT"),
        new TradingPair("UNIUSDT", "UNI", "USDT"),
        new TradingPair("AIAUSDT", "AIA", "USDT"),
        new TradingPair("SHIBUSDT", "SHIB", "USDT"),
        new TradingPair("APTUSDT", "APT", "USDT"),
        new TradingPair("ARBUSDT", "ARB", "USDT"),
        new TradingPair("OPUSDT", "OP", "USDT")
    };

    /// <summary>
    /// Get all trading pairs
    /// </summary>
    public List<TradingPair> GetAllTradingPairs()
    {
        return _tradingPairs;
    }

    /// <summary>
    /// Get trading pair by symbol
    /// </summary>
    public TradingPair? GetTradingPairBySymbol(string symbol)
    {
        return _tradingPairs.FirstOrDefault(p => 
            p.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Search trading pairs by query
    /// </summary>
    public List<TradingPair> SearchTradingPairs(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return _tradingPairs;

        query = query.ToUpperInvariant();
        return _tradingPairs
            .Where(p => 
                p.Symbol.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.BaseAsset.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
