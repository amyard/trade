using MarketParse.Models;
using Microsoft.Extensions.Options;
using Skender.Stock.Indicators;
using System.Collections.Concurrent;

namespace MarketParse.Services;

/// <summary>
/// Simple RSI strategy for monitoring trading pairs
/// </summary>
public class RSISimpleStrategy
{
    private readonly TelegramBotService _telegramService;
    private readonly ILogger<RSISimpleStrategy> _logger;
    private readonly RSIStrategyConfig _config;
    private readonly VolumeFilterService _volumeFilterService;
    
    // Track last alert time to avoid spamming (thread-safe)
    private readonly ConcurrentDictionary<string, DateTime> _lastAlertTime = new();

    public RSISimpleStrategy(
        TelegramBotService telegramService, 
        ILogger<RSISimpleStrategy> logger,
        IOptions<RSIStrategyConfig> config,
        VolumeFilterService volumeFilterService)
    {
        _telegramService = telegramService;
        _logger = logger;
        _config = config.Value;
        _volumeFilterService = volumeFilterService;
        
        _logger.LogInformation(
            $"RSI Strategy initialized: Period={_config.Period}, " +
            $"UpperThreshold={_config.UpperThreshold}, " +
            $"LowerThreshold={_config.LowerThreshold}, " +
            $"AlertCooldown={_config.AlertCooldownMinutes} minutes");
    }

    /// <summary>
    /// Analyze price data and check RSI thresholds
    /// </summary>
    /// <param name="symbol">Trading pair symbol</param>
    /// <param name="candles">List of historical candle data (at least 14+ candles for RSI calculation)</param>
    /// <returns>True if alert was sent, false otherwise</returns>
    public async Task<bool> AnalyzeAsync(string symbol, List<KlineData> candles)
    {
        try
        {
            if (candles == null || candles.Count < _config.Period + 1)
            {
                _logger.LogWarning($"Insufficient data for RSI calculation for {symbol}. Need at least {_config.Period + 1} candles, got {candles?.Count ?? 0}");
                return false;
            }

            // Check cooldown (thread-safe)
            if (_lastAlertTime.TryGetValue(symbol, out var lastAlert))
            {
                var cooldown = TimeSpan.FromMinutes(_config.AlertCooldownMinutes);
                if (DateTime.UtcNow - lastAlert < cooldown)
                {
                    return false;
                }
            }

            // Convert to Quote format for Skender.Stock.Indicators
            var quotes = candles.Select(k => new Quote
            {
                Date = k.OpenTime,
                Open = k.Open,
                High = k.High,
                Low = k.Low,
                Close = k.Close,
                Volume = k.Volume
            }).OrderBy(q => q.Date).ToList();

            // Calculate RSI
            var rsiResults = quotes.GetRsi(_config.Period).ToList();
            var latestRsi = rsiResults.LastOrDefault();

            if (latestRsi?.Rsi == null)
            {
                _logger.LogWarning($"Unable to calculate RSI for {symbol}");
                return false;
            }

            var rsiValue = (decimal)latestRsi.Rsi.Value;
            var currentPrice = candles.Last().Close;

            _logger.LogDebug($"{symbol}: RSI = {rsiValue:F2}, Price = {currentPrice:F4}");

            // Check thresholds
            if (rsiValue > _config.UpperThreshold || rsiValue < _config.LowerThreshold)
            {
                var condition = rsiValue > _config.UpperThreshold ? "Overbought" : "Oversold";
                var emoji = rsiValue > _config.UpperThreshold ? "??" : "??";
                
                // Get 24h volume from cache
                var volume24h = _volumeFilterService.GetCachedVolume(symbol);
                var volumeInfo = volume24h.HasValue 
                    ? $"<b>24h Volume:</b> ${volume24h.Value:N0} USDT\n" 
                    : "";
                
                var message = $"{emoji} <b>RSI Simple Alert - {condition}</b>\n\n" +
                             $"<b>Symbol:</b> {symbol}\n" +
                             $"<b>RSI Value:</b> {rsiValue:F2}\n" +
                             $"<b>Current Price:</b> ${currentPrice:F4}\n" +
                             volumeInfo +
                             $"<b>Time:</b> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";

                var sent = await _telegramService.SendMessageAsync(message);
                
                if (sent)
                {
                    // Update last alert time (thread-safe)
                    _lastAlertTime[symbol] = DateTime.UtcNow;
                    _logger.LogInformation($"? RSI alert sent for {symbol}: RSI={rsiValue:F2}, Price=${currentPrice:F4}");
                }

                return sent;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error analyzing RSI for {symbol}");
            return false;
        }
    }
}
