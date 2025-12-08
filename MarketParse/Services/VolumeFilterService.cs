using Binance.Net.Clients;
using MarketParse.Models;
using Microsoft.Extensions.Options;

namespace MarketParse.Services;

/// <summary>
/// Service for filtering trading pairs based on 24-hour trading volume
/// </summary>
public class VolumeFilterService
{
    private readonly ILogger<VolumeFilterService> _logger;
    private readonly VolumeFilterConfig _config;
    private readonly Dictionary<string, decimal> _volumeCache = new();
    private readonly object _lockObject = new();

    public VolumeFilterService(
        ILogger<VolumeFilterService> logger,
        IOptions<VolumeFilterConfig> config)
    {
        _logger = logger;
        _config = config.Value;
        
        _logger.LogInformation(
            $"Volume Filter Service initialized: MinVolume={_config.MinimumVolumeUsdt:N0} USDT, " +
            $"CheckInterval={_config.CheckIntervalHours}h, Enabled={_config.Enabled}");
    }

    /// <summary>
    /// Check 24-hour volumes for all symbols and filter out those below threshold
    /// </summary>
    /// <param name="symbols">List of symbols to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of symbols that meet the volume requirement</returns>
    public async Task<List<string>> FilterByVolumeAsync(
        List<string> symbols, 
        CancellationToken cancellationToken = default)
    {
        if (!_config.Enabled)
        {
            _logger.LogInformation("Volume filtering is disabled. All symbols will be processed.");
            return symbols;
        }

        if (symbols.Count == 0)
        {
            _logger.LogWarning("No symbols provided for volume filtering");
            return new List<string>();
        }

        _logger.LogInformation($"Checking 24h volumes for {symbols.Count} symbols (Futures)...");

        var validSymbols = new List<string>();
        var filteredSymbols = new List<(string Symbol, decimal Volume)>();
        
        using var client = new BinanceRestClient();

        foreach (var symbol in symbols)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var symbolUpper = symbol.ToUpperInvariant();
                
                // Get 24hr ticker data from Futures API
                var tickerResult = await client.UsdFuturesApi.ExchangeData.GetTickerAsync(symbolUpper);

                if (tickerResult.Success && tickerResult.Data != null)
                {
                    // QuoteVolume is the 24h volume in USDT
                    var volume24h = tickerResult.Data.QuoteVolume;
                    
                    // Update cache
                    lock (_lockObject)
                    {
                        _volumeCache[symbolUpper] = volume24h;
                    }

                    if (volume24h >= _config.MinimumVolumeUsdt)
                    {
                        validSymbols.Add(symbol.ToLowerInvariant());
                        _logger.LogDebug(
                            $"? {symbolUpper}: Volume=${volume24h:N0} (above threshold)");
                    }
                    else
                    {
                        filteredSymbols.Add((symbolUpper, volume24h));
                        _logger.LogDebug(
                            $"? {symbolUpper}: Volume=${volume24h:N0} (below ${_config.MinimumVolumeUsdt:N0} threshold)");
                    }
                }
                else
                {
                    _logger.LogWarning(
                        $"? Failed to get volume for {symbolUpper}: {tickerResult.Error?.Message ?? "Unknown error"}");
                }

                // Avoid rate limiting
                await Task.Delay(50, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking volume for {symbol}");
            }
        }

        // Log summary
        _logger.LogInformation(
            $"Volume filtering complete: {validSymbols.Count}/{symbols.Count} symbols passed " +
            $"(threshold: ${_config.MinimumVolumeUsdt:N0} USDT)");

        if (filteredSymbols.Count > 0)
        {
            _logger.LogInformation(
                $"Filtered out {filteredSymbols.Count} low-volume symbols: " +
                string.Join(", ", filteredSymbols
                    .OrderBy(s => s.Volume)
                    .Select(s => $"{s.Symbol}(${s.Volume:N0})")));
        }

        return validSymbols;
    }

    /// <summary>
    /// Get cached 24h volume for a symbol
    /// </summary>
    /// <param name="symbol">Trading pair symbol</param>
    /// <returns>Cached volume in USDT, or null if not found</returns>
    public decimal? GetCachedVolume(string symbol)
    {
        lock (_lockObject)
        {
            if (_volumeCache.TryGetValue(symbol.ToUpperInvariant(), out var volume))
            {
                return volume;
            }
        }
        return null;
    }

    /// <summary>
    /// Get all cached volumes
    /// </summary>
    /// <returns>Dictionary of symbol -> 24h volume in USDT</returns>
    public Dictionary<string, decimal> GetAllCachedVolumes()
    {
        lock (_lockObject)
        {
            return new Dictionary<string, decimal>(_volumeCache);
        }
    }

    /// <summary>
    /// Clear volume cache
    /// </summary>
    public void ClearCache()
    {
        lock (_lockObject)
        {
            _volumeCache.Clear();
        }
        _logger.LogDebug("Volume cache cleared");
    }
}
