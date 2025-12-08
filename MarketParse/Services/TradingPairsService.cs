using MarketParse.Models;
using Microsoft.Extensions.Options;

namespace MarketParse.Services;

/// <summary>
/// Service for managing trading pairs list from configuration
/// </summary>
public class TradingPairsService
{
    private readonly List<TradingPair> _tradingPairs = new();
    private readonly ILogger<TradingPairsService> _logger;

    public TradingPairsService(
        IOptions<TradingPairsConfig> config,
        ILogger<TradingPairsService> logger)
    {
        _logger = logger;
        
        // Load trading pairs from configuration
        var tradingPairsConfig = config.Value;
        
        if (tradingPairsConfig?.TradingPairs != null)
        {
            _tradingPairs = tradingPairsConfig.TradingPairs
                .Select(p =>
                {
                    var baseAsset = p.Symbol.Replace("USDT", "");
                    return new TradingPair(
                        p.Symbol,
                        baseAsset,
                        "USDT",
                        baseAsset); // Use baseAsset as FullName
                })
                .ToList();
            
            _logger.LogInformation($"Loaded {_tradingPairs.Count} trading pairs from pairs.json");
        }
        else
        {
            _logger.LogWarning("No trading pairs found in configuration, using empty list");
        }
    }

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
                p.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (p.FullName != null && p.FullName.Contains(query, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }
}
