using Binance.Net.Clients;
using Binance.Net.Interfaces;
using CryptoExchange.Net.Objects.Sockets;
using MarketParse.Models;
using Microsoft.Extensions.Options;

namespace MarketParse.Services;

/// <summary>
/// Background service for monitoring trading pairs via Binance WebSocket (Futures)
/// </summary>
public class BinanceWebSocketBackgroundService : BackgroundService
{
    private readonly ILogger<BinanceWebSocketBackgroundService> _logger;
    private readonly TradingPairsConfig _config;
    private readonly RSISimpleStrategy _rsiStrategy;
    private readonly TelegramBotService _telegramService;
    private readonly IServiceProvider _serviceProvider;
    
    // Store candle data for each symbol (keep last 50 candles for RSI calculation)
    private readonly Dictionary<string, List<KlineData>> _candleData = new();
    private readonly int _maxCandles = 50;
    private readonly object _lockObject = new();
    
    // Track valid and invalid symbols
    private readonly HashSet<string> _validSymbols = new();
    private readonly HashSet<string> _invalidSymbols = new();
    
    // WebSocket subscription
    private UpdateSubscription? _subscription;
    
    // Timer for periodic RSI checks
    private Timer? _rsiCheckTimer;

    public BinanceWebSocketBackgroundService(
        ILogger<BinanceWebSocketBackgroundService> logger,
        IOptions<TradingPairsConfig> config,
        IOptions<RSIStrategyConfig> rsiConfig,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _config = config.Value;
        _serviceProvider = serviceProvider;
        
        // Get singleton services
        _telegramService = serviceProvider.GetRequiredService<TelegramBotService>();
        
        // Create RSI strategy with configuration
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        var rsiLogger = loggerFactory.CreateLogger<RSISimpleStrategy>();
        _rsiStrategy = new RSISimpleStrategy(_telegramService, rsiLogger, rsiConfig);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Binance WebSocket Background Service is starting (Futures API)...");

        // Wait a bit for the application to fully start
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        try
        {
            // Test Telegram connection
            var telegramConnected = await _telegramService.TestConnectionAsync();
            if (!telegramConnected)
            {
                _logger.LogWarning("Telegram bot connection failed. Alerts may not be sent.");
            }

            // Get trading pairs from configuration
            var symbols = _config.TradingPairs.Select(p => p.Symbol.ToLowerInvariant()).ToList();
            
            if (symbols.Count == 0)
            {
                _logger.LogWarning("No trading pairs configured in pairs.json");
                return;
            }

            _logger.LogInformation($"Processing {symbols.Count} trading pairs from configuration (Futures)...");

            // Initialize candle data storage
            foreach (var symbol in symbols)
            {
                _candleData[symbol.ToUpperInvariant()] = new List<KlineData>();
            }

            // Load initial historical data (this will filter out invalid symbols)
            await LoadHistoricalDataAsync(symbols, stoppingToken);

            // Log statistics
            _logger.LogInformation(
                $"Symbol validation complete: {_validSymbols.Count} valid, {_invalidSymbols.Count} invalid");
            
            if (_invalidSymbols.Count > 0)
            {
                _logger.LogWarning(
                    $"Invalid symbols (not available on Binance Futures): {string.Join(", ", _invalidSymbols.Select(s => s.ToUpperInvariant()))}");
            }

            if (_validSymbols.Count == 0)
            {
                _logger.LogError("No valid trading pairs found. Service will not start monitoring.");
                return;
            }

            // Subscribe to kline updates only for valid pairs
            var validSymbolsList = _validSymbols.ToList();
            await SubscribeToKlineUpdatesAsync(validSymbolsList, stoppingToken);

            // Start timer for RSI checks every 1 second
            _rsiCheckTimer = new Timer(
                async _ => await CheckRsiForAllSymbols(),
                null,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1)
            );

            _logger.LogInformation(
                $"? Background service started successfully. Monitoring {_validSymbols.Count} pairs for RSI alerts (Futures, checking every 1 second)");

            // Keep the service running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                
                lock (_lockObject)
                {
                    var pairsWithData = _candleData.Count(kvp => kvp.Value.Count >= 15);
                    _logger.LogInformation(
                        $"?? Service status: Monitoring {_validSymbols.Count} Futures pairs, " +
                        $"{pairsWithData} pairs with sufficient data for RSI calculation");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Binance WebSocket Background Service");
        }
        finally
        {
            // Cleanup
            _rsiCheckTimer?.Dispose();
            await UnsubscribeAllAsync();
        }
    }

    /// <summary>
    /// Check RSI for all symbols (called every second)
    /// </summary>
    private async Task CheckRsiForAllSymbols()
    {
        try
        {
            List<KeyValuePair<string, List<KlineData>>> symbolsToCheck;
            
            lock (_lockObject)
            {
                // Get snapshot of all symbols with sufficient data
                symbolsToCheck = _candleData
                    .Where(kvp => kvp.Value.Count >= 15 && _validSymbols.Contains(kvp.Key.ToLowerInvariant()))
                    .Select(kvp => new KeyValuePair<string, List<KlineData>>(kvp.Key, new List<KlineData>(kvp.Value)))
                    .ToList();
            }

            // Check RSI for each symbol in parallel
            var tasks = symbolsToCheck.Select(async kvp =>
            {
                try
                {
                    await _rsiStrategy.AnalyzeAsync(kvp.Key, kvp.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error checking RSI for {kvp.Key}");
                }
            });

            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CheckRsiForAllSymbols");
        }
    }

    /// <summary>
    /// Load historical kline data for initial RSI calculation (Futures API)
    /// </summary>
    private async Task LoadHistoricalDataAsync(List<string> symbols, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading historical data for RSI calculation (Futures API)...");

        using var client = new BinanceRestClient();
        int successCount = 0;
        int errorCount = 0;

        foreach (var symbol in symbols)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                var symbolUpper = symbol.ToUpperInvariant();
                
                // Get last 50 1-minute candles from Futures API
                var klineResult = await client.UsdFuturesApi.ExchangeData.GetKlinesAsync(
                    symbolUpper,
                    Binance.Net.Enums.KlineInterval.OneMinute,
                    limit: _maxCandles);

                if (klineResult.Success && klineResult.Data != null && klineResult.Data.Any())
                {
                    var candles = klineResult.Data.Select(k => new KlineData
                    {
                        Symbol = symbolUpper,
                        OpenTime = k.OpenTime,
                        Open = k.OpenPrice,
                        High = k.HighPrice,
                        Low = k.LowPrice,
                        Close = k.ClosePrice,
                        Volume = k.Volume,
                        CloseTime = k.CloseTime
                    }).ToList();

                    lock (_lockObject)
                    {
                        _candleData[symbolUpper] = candles;
                    }

                    _validSymbols.Add(symbol.ToLowerInvariant());
                    successCount++;
                    
                    _logger.LogDebug($"? Loaded {candles.Count} candles for {symbolUpper} (Futures)");
                }
                else
                {
                    _invalidSymbols.Add(symbol.ToLowerInvariant());
                    errorCount++;
                    
                    var errorMessage = klineResult.Error?.Message ?? "No data available";
                    _logger.LogDebug($"? Skipping {symbolUpper}: {errorMessage}");
                }

                // Avoid rate limiting
                await Task.Delay(100, cancellationToken);
            }
            catch (Exception ex)
            {
                _invalidSymbols.Add(symbol.ToLowerInvariant());
                errorCount++;
                _logger.LogDebug($"? Error loading {symbol.ToUpperInvariant()}: {ex.Message}");
            }
        }

        _logger.LogInformation(
            $"Historical data loading completed (Futures): {successCount} successful, {errorCount} failed");
    }

    /// <summary>
    /// Subscribe to real-time kline updates via WebSocket (Futures API)
    /// </summary>
    private async Task SubscribeToKlineUpdatesAsync(List<string> symbols, CancellationToken cancellationToken)
    {
        if (symbols.Count == 0)
        {
            _logger.LogWarning("No symbols to subscribe to WebSocket");
            return;
        }

        _logger.LogInformation($"Subscribing to WebSocket kline updates for {symbols.Count} pairs (Futures)...");

        var socketClient = new BinanceSocketClient();

        try
        {
            // Subscribe to kline updates for all symbols (1-minute interval) on Futures
            var subscriptionResult = await socketClient.UsdFuturesApi.SubscribeToKlineUpdatesAsync(
                symbols,
                Binance.Net.Enums.KlineInterval.OneMinute,
                data => OnKlineUpdate(data.Data));

            if (subscriptionResult.Success && subscriptionResult.Data != null)
            {
                _subscription = subscriptionResult.Data;
                _logger.LogInformation($"? Successfully subscribed to WebSocket for {symbols.Count} Futures pairs");
            }
            else
            {
                _logger.LogError($"? Failed to subscribe to WebSocket: {subscriptionResult.Error?.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "? Error subscribing to WebSocket updates");
        }
    }

    /// <summary>
    /// Handle incoming kline update from WebSocket (updates current candle data in real-time)
    /// </summary>
    private void OnKlineUpdate(IBinanceStreamKlineData klineData)
    {
        try
        {
            var symbol = klineData.Symbol.ToUpperInvariant();
            var kline = klineData.Data;

            // Only process valid symbols
            if (!_validSymbols.Contains(symbol.ToLowerInvariant()))
            {
                return;
            }

            var candle = new KlineData
            {
                Symbol = symbol,
                OpenTime = kline.OpenTime,
                Open = kline.OpenPrice,
                High = kline.HighPrice,
                Low = kline.LowPrice,
                Close = kline.ClosePrice,
                Volume = kline.Volume,
                CloseTime = kline.CloseTime,
                IsClosed = kline.Final
            };

            lock (_lockObject)
            {
                if (!_candleData.ContainsKey(symbol))
                {
                    _candleData[symbol] = new List<KlineData>();
                }

                // If candle is closed, add as new candle
                if (kline.Final)
                {
                    _candleData[symbol].Add(candle);
                    
                    // Keep only last N candles
                    if (_candleData[symbol].Count > _maxCandles)
                    {
                        _candleData[symbol].RemoveAt(0);
                    }
                    
                    _logger.LogDebug($"??? New closed candle for {symbol} (Futures): Close={candle.Close:F4}");
                }
                else
                {
                    // Update the last (current) candle with real-time data
                    if (_candleData[symbol].Count > 0)
                    {
                        var lastIndex = _candleData[symbol].Count - 1;
                        var lastCandle = _candleData[symbol][lastIndex];
                        
                        // Only update if it's the same candle (same open time)
                        if (lastCandle.OpenTime == candle.OpenTime)
                        {
                            _candleData[symbol][lastIndex] = candle;
                        }
                        else
                        {
                            // This is a new unclosed candle, add it
                            _candleData[symbol].Add(candle);
                            
                            if (_candleData[symbol].Count > _maxCandles)
                            {
                                _candleData[symbol].RemoveAt(0);
                            }
                        }
                    }
                    else
                    {
                        // First candle for this symbol
                        _candleData[symbol].Add(candle);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing kline update for {klineData.Symbol}");
        }
    }

    /// <summary>
    /// Unsubscribe from all WebSocket connections
    /// </summary>
    private async Task UnsubscribeAllAsync()
    {
        _logger.LogInformation("Unsubscribing from all WebSocket connections...");

        if (_subscription != null)
        {
            try
            {
                await _subscription.CloseAsync();
                _logger.LogInformation("? WebSocket subscription closed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing WebSocket subscription");
            }
            
            _subscription = null;
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("?? Binance WebSocket Background Service is stopping...");
        
        _rsiCheckTimer?.Dispose();
        await UnsubscribeAllAsync();
        
        _logger.LogInformation("? Background service stopped successfully");
        
        await base.StopAsync(cancellationToken);
    }
}
