using MarketParse.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace MarketParse.Services;

/// <summary>
/// Service for managing trading pairs list from configuration
/// </summary>
public class TradingPairsService
{
    private readonly List<TradingPair> _tradingPairs = new();
    private readonly ILogger<TradingPairsService> _logger;
    private readonly IWebHostEnvironment _environment;
    private readonly string _pairsJsonPath;

    public TradingPairsService(
        IOptions<TradingPairsConfig> config,
        ILogger<TradingPairsService> logger,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
        _pairsJsonPath = Path.Combine(_environment.ContentRootPath, "pairs.json");
        
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
                        baseAsset,
                        p.Priority); // Pass Priority from configuration
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

    /// <summary>
    /// Update priority for a trading pair and save to pairs.json
    /// </summary>
    public async Task<bool> UpdatePriorityAsync(string symbol, Priority newPriority)
    {
        try
        {
            // Update in memory
            var pair = _tradingPairs.FirstOrDefault(p => 
                p.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
            
            if (pair == null)
            {
                _logger.LogWarning($"Trading pair {symbol} not found");
                return false;
            }

            pair.Priority = newPriority;

            // Save to pairs.json
            var config = new TradingPairsConfig
            {
                TradingPairs = _tradingPairs.Select(p => new TradingPairInfo
                {
                    Symbol = p.Symbol,
                    Priority = p.Priority
                }).ToList()
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var jsonString = JsonSerializer.Serialize(config, options);
            await File.WriteAllTextAsync(_pairsJsonPath, jsonString);

            _logger.LogInformation($"Updated priority for {symbol} to {newPriority} and saved to pairs.json");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating priority for {symbol}");
            return false;
        }
    }
}
