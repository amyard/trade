namespace MarketParse.Models;

/// <summary>
/// Configuration for volume-based filtering of trading pairs
/// </summary>
public class VolumeFilterConfig
{
    /// <summary>
    /// Minimum 24-hour volume in USDT to include a trading pair (default: 20,000,000)
    /// </summary>
    public decimal MinimumVolumeUsdt { get; set; } = 20_000_000m;
    
    /// <summary>
    /// How often to check volumes in hours (default: 1 hour)
    /// </summary>
    public int CheckIntervalHours { get; set; } = 1;
    
    /// <summary>
    /// Enable volume filtering (default: true)
    /// </summary>
    public bool Enabled { get; set; } = true;
}
