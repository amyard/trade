namespace MarketParse.Models;

/// <summary>
/// Model for storing Binance kline (candlestick) data
/// </summary>
public class KlineData
{
    /// <summary>
    /// Kline open time
    /// </summary>
    public DateTime OpenTime { get; set; }
    
    /// <summary>
    /// Open price
    /// </summary>
    public decimal Open { get; set; }
    
    /// <summary>
    /// High price
    /// </summary>
    public decimal High { get; set; }
    
    /// <summary>
    /// Low price
    /// </summary>
    public decimal Low { get; set; }
    
    /// <summary>
    /// Close price
    /// </summary>
    public decimal Close { get; set; }
    
    /// <summary>
    /// Volume
    /// </summary>
    public decimal Volume { get; set; }
    
    /// <summary>
    /// Kline close time
    /// </summary>
    public DateTime CloseTime { get; set; }
    
    /// <summary>
    /// Trading pair symbol
    /// </summary>
    public string Symbol { get; set; } = string.Empty;
    
    /// <summary>
    /// Quote volume (volume in quote asset, e.g., USDT)
    /// </summary>
    public decimal QuoteVolume { get; set; }
    
    /// <summary>
    /// Number of trades
    /// </summary>
    public int TradeCount { get; set; }
    
    /// <summary>
    /// Whether this candle is closed/finalized
    /// </summary>
    public bool IsClosed { get; set; }
}
