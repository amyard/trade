namespace MarketParse.Models;

/// <summary>
/// Configuration for RSI strategy thresholds
/// </summary>
public class RSIStrategyConfig
{
    /// <summary>
    /// RSI period for calculation (default: 14)
    /// </summary>
    public int Period { get; set; } = 14;
    
    /// <summary>
    /// Upper threshold - overbought level (default: 67)
    /// </summary>
    public decimal UpperThreshold { get; set; } = 67;
    
    /// <summary>
    /// Lower threshold - oversold level (default: 23)
    /// </summary>
    public decimal LowerThreshold { get; set; } = 23;
    
    /// <summary>
    /// Alert cooldown in minutes to prevent spam (default: 5)
    /// </summary>
    public int AlertCooldownMinutes { get; set; } = 5;
}
