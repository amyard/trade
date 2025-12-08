using Binance.Net.Clients;
using Binance.Net.Interfaces;
using CryptoExchange.Net.Sockets;
using CryptoExchange.Net.Objects.Sockets;
using Binance.Net.Enums;
using Skender.Stock.Indicators;
using MarketParse.Models;

namespace MarketParse.Services;

public class BinanceFuturesService
{
    private readonly BinanceRestClient _restClient;
    private readonly BinanceSocketClient _socketClient;
    private readonly ILogger<BinanceFuturesService> _logger;

    public BinanceFuturesService(ILogger<BinanceFuturesService> logger)
    {
        _logger = logger;
        _restClient = new BinanceRestClient();
        _socketClient = new BinanceSocketClient();
    }

    /// <summary>
    /// Get current price for a symbol
    /// </summary>
    public async Task<decimal?> GetCurrentPriceAsync(string symbol)
    {
        try
        {
            var priceResult = await _restClient.UsdFuturesApi.ExchangeData.GetPriceAsync(symbol);
            
            if (priceResult.Success && priceResult.Data != null)
            {
                return priceResult.Data.Price;
            }
            else
            {
                _logger.LogError($"Error getting price for {symbol}: {priceResult.Error?.Message}");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error while getting price for {symbol}");
            return null;
        }
    }

    /// <summary>
    /// Get current prices for multiple symbols
    /// </summary>
    public async Task<Dictionary<string, decimal>> GetCurrentPricesAsync(IEnumerable<string> symbols)
    {
        var result = new Dictionary<string, decimal>();

        try
        {
            var pricesResult = await _restClient.UsdFuturesApi.ExchangeData.GetPricesAsync();
            
            if (pricesResult.Success && pricesResult.Data != null)
            {
                foreach (var symbol in symbols)
                {
                    var price = pricesResult.Data.FirstOrDefault(p => p.Symbol == symbol);
                    if (price != null)
                    {
                        result[symbol] = price.Price;
                    }
                }
                
                _logger.LogInformation($"Retrieved prices for {result.Count} symbols");
            }
            else
            {
                _logger.LogError($"Error getting prices: {pricesResult.Error?.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting prices");
        }

        return result;
    }

    /// <summary>
    /// Subscribe to real-time price updates for multiple symbols via WebSocket
    /// </summary>
    public async Task<UpdateSubscription?> SubscribeToPriceUpdatesAsync(
        IEnumerable<string> symbols,
        Action<string, decimal>? onPriceUpdate = null)
    {
        try
        {
            var symbolsList = symbols.ToList();
            
            // Subscribe to ticker price updates for multiple symbols
            var subscriptionResult = await _socketClient.UsdFuturesApi.SubscribeToTickerUpdatesAsync(
                symbolsList,
                data =>
                {
                    var symbol = data.Data.Symbol;
                    var price = data.Data.LastPrice;
                    
                    _logger.LogDebug($"Price update: {symbol} = {price}");
                    onPriceUpdate?.Invoke(symbol, price);
                }
            );

            if (subscriptionResult.Success)
            {
                _logger.LogInformation($"Successfully subscribed to price updates for {symbolsList.Count} symbols");
                return subscriptionResult.Data;
            }
            else
            {
                _logger.LogError($"Price WebSocket subscription error: {subscriptionResult.Error?.Message}");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while subscribing to price updates");
            return null;
        }
    }

    /// <summary>
    /// Get kline data with flexible parameters for charting
    /// </summary>
    public async Task<List<KlineData>> GetKlineDataAsync(
        string symbol,
        KlineInterval interval,
        DateTime? startTime = null,
        DateTime? endTime = null,
        int? limit = null)
    {
        var result = new List<KlineData>();

        try
        {
            var klinesResult = await _restClient.UsdFuturesApi.ExchangeData.GetKlinesAsync(
                symbol: symbol,
                interval: interval,
                startTime: startTime,
                endTime: endTime,
                limit: limit
            );

            if (klinesResult.Success && klinesResult.Data != null)
            {
                result = klinesResult.Data.Select(k => new KlineData
                {
                    OpenTime = k.OpenTime,
                    CloseTime = k.CloseTime,
                    Symbol = symbol,
                    Open = k.OpenPrice,
                    High = k.HighPrice,
                    Low = k.LowPrice,
                    Close = k.ClosePrice,
                    Volume = k.Volume,
                    QuoteVolume = k.QuoteVolume,
                    TradeCount = k.TradeCount
                }).ToList();

                _logger.LogInformation($"Retrieved {result.Count} klines for {symbol}");
            }
            else
            {
                _logger.LogError($"Error getting klines: {klinesResult.Error?.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting kline data");
        }

        return result;
    }

    /// <summary>
    /// Get historical Kline data for the last 120 minutes for SOL/USDT pair on Futures market
    /// </summary>
    public async Task<List<KlineData>> GetHistoricalKlinesAsync(string symbol = "SOLUSDT", int minutes = 120)
    {
        var endTime = DateTime.UtcNow;
        var startTime = endTime.AddMinutes(-minutes);

        return await GetKlineDataAsync(
            symbol,
            KlineInterval.OneMinute,
            startTime,
            endTime,
            minutes
        );
    }

    /// <summary>
    /// Subscribe to Kline updates via WebSocket for SOL/USDT pair on Futures market
    /// </summary>
    public async Task<UpdateSubscription?> SubscribeToKlineUpdatesAsync(
        string symbol = "SOLUSDT",
        Action<KlineData>? onKlineUpdate = null)
    {
        try
        {
            // Subscribe to 1-minute Kline updates via WebSocket for USD-M Futures
            var subscriptionResult = await _socketClient.UsdFuturesApi.SubscribeToKlineUpdatesAsync(
                symbol: symbol,
                interval: Binance.Net.Enums.KlineInterval.OneMinute,
                onMessage: data =>
                {
                    var kline = data.Data;
                    var klineData = new KlineData
                    {
                        OpenTime = kline.Data.OpenTime,
                        CloseTime = kline.Data.CloseTime,
                        Symbol = kline.Symbol,
                        Open = kline.Data.OpenPrice,
                        High = kline.Data.HighPrice,
                        Low = kline.Data.LowPrice,
                        Close = kline.Data.ClosePrice,
                        Volume = kline.Data.Volume,
                        QuoteVolume = kline.Data.QuoteVolume,
                        TradeCount = kline.Data.TradeCount,
                        IsClosed = kline.Data.Final
                    };

                    _logger.LogInformation($"WebSocket update: {klineData.Symbol} - Close: {klineData.Close}, Volume: {klineData.Volume}, IsClosed: {klineData.IsClosed}");
                    
                    onKlineUpdate?.Invoke(klineData);
                }
            );

            if (subscriptionResult.Success)
            {
                _logger.LogInformation($"Successfully subscribed to WebSocket for {symbol} Futures");
                return subscriptionResult.Data;
            }
            else
            {
                _logger.LogError($"WebSocket subscription error: {subscriptionResult.Error?.Message}");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while subscribing to WebSocket");
            return null;
        }
    }

    /// <summary>
    /// Unsubscribe from WebSocket updates
    /// </summary>
    public async Task UnsubscribeAsync(UpdateSubscription? subscription)
    {
        if (subscription is not null)
        {
            await subscription.CloseAsync();
            _logger.LogInformation("WebSocket subscription closed");
        }
    }

    /// <summary>
    /// Get 24-hour trade count for a symbol
    /// </summary>
    public async Task<long?> Get24HourTradeCountAsync(string symbol)
    {
        try
        {
            // Get 24h statistics which includes trade count
            var tickerResult = await _restClient.UsdFuturesApi.ExchangeData.GetTickerAsync(symbol);
            
            if (tickerResult.Success && tickerResult.Data != null)
            {
                // IBinance24HPrice contains Volume property which represents trade count
                return (long)tickerResult.Data.Volume;
            }
            else
            {
                _logger.LogError($"Error getting 24h trade count for {symbol}: {tickerResult.Error?.Message}");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error while getting 24h trade count for {symbol}");
            return null;
        }
    }

    /// <summary>
    /// Get 24-hour trade counts for multiple symbols
    /// </summary>
    public async Task<Dictionary<string, long>> Get24HourTradeCountsAsync(IEnumerable<string> symbols)
    {
        var result = new Dictionary<string, long>();

        try
        {
            var tickersResult = await _restClient.UsdFuturesApi.ExchangeData.GetTickersAsync();
            
            if (tickersResult.Success && tickersResult.Data != null)
            {
                foreach (var symbol in symbols)
                {
                    var ticker = tickersResult.Data.FirstOrDefault(t => t.Symbol == symbol);
                    if (ticker != null)
                    {
                        // Use volume as trade count approximation
                        result[symbol] = (long)ticker.Volume;
                    }
                }
                
                _logger.LogInformation($"Retrieved 24h trade counts for {result.Count} symbols");
            }
            else
            {
                _logger.LogError($"Error getting 24h trade counts: {tickersResult.Error?.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting 24h trade counts");
        }

        return result;
    }

    /// <summary>
    /// Get 24-hour volume in USDT for a symbol
    /// </summary>
    public async Task<decimal?> Get24HourVolumeAsync(string symbol)
    {
        try
        {
            var tickerResult = await _restClient.UsdFuturesApi.ExchangeData.GetTickerAsync(symbol);
            
            if (tickerResult.Success && tickerResult.Data != null)
            {
                // QuoteVolume represents the volume in quote currency (USDT)
                return tickerResult.Data.QuoteVolume;
            }
            else
            {
                _logger.LogError($"Error getting 24h volume for {symbol}: {tickerResult.Error?.Message}");
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error while getting 24h volume for {symbol}");
            return null;
        }
    }

    /// <summary>
    /// Get 24-hour volumes in USDT for multiple symbols
    /// </summary>
    public async Task<Dictionary<string, decimal>> Get24HourVolumesAsync(IEnumerable<string> symbols)
    {
        var result = new Dictionary<string, decimal>();

        try
        {
            var tickersResult = await _restClient.UsdFuturesApi.ExchangeData.GetTickersAsync();
            
            if (tickersResult.Success && tickersResult.Data != null)
            {
                foreach (var symbol in symbols)
                {
                    var ticker = tickersResult.Data.FirstOrDefault(t => t.Symbol == symbol);
                    if (ticker != null)
                    {
                        // QuoteVolume is the volume in USDT
                        result[symbol] = ticker.QuoteVolume;
                    }
                }
                
                _logger.LogInformation($"Retrieved 24h volumes for {result.Count} symbols");
            }
            else
            {
                _logger.LogError($"Error getting 24h volumes: {tickersResult.Error?.Message}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting 24h volumes");
        }

        return result;
    }

    /// <summary>
    /// Calculate RSI for a symbol using recent kline data
    /// </summary>
    public async Task<decimal?> CalculateRsiAsync(string symbol, int period = 14)
    {
        try
        {
            // Get enough candles to calculate RSI properly
            // RSI needs at least period + 1 candles, but we get more for accuracy (warmup period)
            var limit = 200; // Get 200 candles for proper RSI calculation
            
            // Use 1-minute candles as requested
            var klines = await GetKlineDataAsync(symbol, KlineInterval.OneMinute, limit: limit);
            
            if (klines == null || klines.Count < period + 1)
            {
                _logger.LogWarning($"Not enough data to calculate RSI for {symbol}. Got {klines?.Count ?? 0} candles, need at least {period + 1}");
                return null;
            }

            // Convert to Quote format for Skender library
            var quotes = klines.Select(k => new Quote
            {
                Date = k.OpenTime,
                Open = k.Open,
                High = k.High,
                Low = k.Low,
                Close = k.Close,  // RSI source is Close price
                Volume = k.Volume
            }).OrderBy(q => q.Date).ToList();

            // Calculate RSI with period 14
            var rsiResults = quotes.GetRsi(period).ToList();
            
            // Get the latest RSI value that has a valid result
            var latestRsi = rsiResults.LastOrDefault(r => r.Rsi.HasValue);

            if (latestRsi?.Rsi != null)
            {
                _logger.LogDebug($"RSI for {symbol}: {latestRsi.Rsi.Value:F2} (Date: {latestRsi.Date})");
                return (decimal)latestRsi.Rsi.Value;
            }

            _logger.LogWarning($"No valid RSI calculated for {symbol}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error calculating RSI for {symbol}");
            return null;
        }
    }

    /// <summary>
    /// Calculate RSI for multiple symbols
    /// </summary>
    public async Task<Dictionary<string, decimal>> CalculateRsiForSymbolsAsync(IEnumerable<string> symbols, int period = 14)
    {
        var result = new Dictionary<string, decimal>();
        
        try
        {
            var tasks = symbols.Select(async symbol =>
            {
                var rsi = await CalculateRsiAsync(symbol, period);
                return new { Symbol = symbol, Rsi = rsi };
            });

            var results = await Task.WhenAll(tasks);

            foreach (var item in results)
            {
                if (item.Rsi.HasValue)
                {
                    result[item.Symbol] = item.Rsi.Value;
                }
            }

            _logger.LogInformation($"Calculated RSI for {result.Count} symbols");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating RSI for multiple symbols");
        }

        return result;
    }

    public void Dispose()
    {
        _restClient?.Dispose();
        _socketClient?.Dispose();
    }
}
