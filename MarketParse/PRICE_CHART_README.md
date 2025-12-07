# Interactive Price Chart Feature

## Overview
Added interactive price chart with zoom, pan, and historical data loading capabilities for trading pair detail pages.

## Features

### 1. Interactive Chart Display
- **Time Range Selection**: 15min, 30min, 1h, 2h, 4h, 8h, 24h
- **Interval Selection**: 1min, 3min, 5min, 15min, 30min, 1hour
- **Three Lines Display**:
  - Close Price (main line - blue)
  - High Price (green line)
  - Low Price (pink line)

### 2. Chart Interactions
- **Zoom In/Out**: Mouse wheel or toolbar buttons
- **Pan**: Click and drag to move through time
- **Select Area**: Click and drag to zoom into specific area
- **Reset Zoom**: Button to return to original view
- **Download**: Export chart as image

### 3. Historical Data Loading
- **"Load Earlier" Button**: Fetches additional historical data
- **Seamless Integration**: New data prepended to existing chart
- **Automatic Chart Update**: Chart refreshes with new data
- **No Data Loss**: Previously loaded data is retained

### 4. Real-Time Updates
- **WebSocket Integration**: Chart updates automatically with new kline data
- **Smooth Updates**: New candles added or existing ones updated
- **Visual Continuity**: Chart maintains zoom and pan state during updates

## Technical Implementation

### BinanceFuturesService Updates

Added flexible kline data retrieval method:
```csharp
public async Task<List<KlineData>> GetKlineDataAsync(
    string symbol,
    KlineInterval interval,
    DateTime? startTime = null,
    DateTime? endTime = null,
    int? limit = null)
```

Features:
- Flexible time range (startTime/endTime)
- Configurable interval (1min to 1hour)
- Optional limit parameter
- Returns KlineData list

### PriceChart Component

Location: `Components/Shared/PriceChart.razor`

#### Properties:
- `Symbol` (Parameter): Trading pair symbol
- `InitialMinutes` (Parameter): Initial time range in minutes (default: 60)

#### Key Methods:
- `LoadChartData()`: Load initial chart data
- `LoadMoreHistoricalData()`: Load earlier historical data
- `AddRealtimeData(KlineData)`: Add/update real-time data
- `ResetZoom()`: Reset chart zoom to default
- `OnTimeRangeChanged()`: Handle time range selection
- `OnIntervalChanged()`: Handle interval selection

#### Chart Options:
```csharp
- Toolbar: Download, Selection, Zoom, Pan, Reset tools
- Animations: Smooth transitions enabled
- X-Axis: DateTime format (HH:mm)
- Y-Axis: Dollar format ($XX.XX)
- Tooltip: Shows date and price on hover
- Stroke: Smooth curves, 2px width
- Grid: Visible for better reading
```

### Integration with TradingPairDetails

The chart is integrated into the trading pair details page:
```razor
<PriceChart @ref="priceChart" 
            Symbol="@tradingPair.Symbol" 
            InitialMinutes="60" />
```

Real-time updates flow:
1. WebSocket receives new kline data
2. `OnKlineUpdate()` method called
3. Data passed to `priceChart.AddRealtimeData()`
4. Chart updates automatically

## User Experience

### Initial Load:
1. Page loads with 60 minutes of 1-minute candles
2. Chart displays immediately with three lines (Close, High, Low)
3. Interactive toolbar visible at top-right

### Time Range Change:
1. User selects different time range from dropdown
2. Chart reloads with new data
3. All loaded historical data replaced

### Interval Change:
1. User selects different interval (e.g., 5min instead of 1min)
2. Chart reloads with aggregated data
3. Fewer but wider candles displayed

### Loading Earlier Data:
1. User clicks "Load Earlier" button
2. Additional data loaded for previous time period
3. Data prepended to chart
4. Current view maintained (no auto-scroll)

### Zooming:
1. **Mouse Wheel**: Zoom in/out at cursor position
2. **Drag Select**: Select area to zoom into
3. **Toolbar Buttons**: Zoom in/out buttons
4. **Reset Button**: Return to full view

### Panning:
1. Click and drag chart to move left/right
2. View historical or future data
3. Smooth animation during pan

## Chart Controls

### Top Row Controls:
```
[Time Range ?] [Interval ?] [Load Earlier] [Reset Zoom]
```

### Toolbar (Top-Right of Chart):
```
[Download] [Select] [Zoom] [+] [-] [Pan] [Reset]
```

### Bottom Info:
```
Showing 60 data points | From: 2025-01-07 10:00 | To: 2025-01-07 11:00
```

## Data Flow

### Initial Load:
```
Page Load ? LoadChartData() ? GetKlineDataAsync() ? UpdateChartData() ? Render Chart
```

### Load More:
```
Click "Load Earlier" ? LoadMoreHistoricalData() ? GetKlineDataAsync() ? 
InsertRange(0, data) ? UpdateChartData() ? UpdateSeriesAsync()
```

### Real-Time Update:
```
WebSocket Update ? OnKlineUpdate() ? priceChart.AddRealtimeData() ? 
UpdateChartData() ? UpdateSeriesAsync()
```

## Performance Optimizations

1. **Efficient Data Structure**: List maintained in chronological order
2. **Smart Updates**: Existing candles updated, new ones added
3. **Lazy Loading**: Historical data loaded on demand
4. **Chart Caching**: ApexCharts handles efficient rendering
5. **State Management**: Minimal re-renders with StateHasChanged()

## Dependencies

- **Blazor-ApexCharts** (v6.0.2): Chart library
- **Binance.Net**: Market data provider
- **ApexCharts.js**: JavaScript charting engine

## Files Modified/Created

### Created:
- `Components/Shared/PriceChart.razor` - Main chart component

### Modified:
- `Services/BinanceFuturesService.cs` - Added GetKlineDataAsync method
- `Components/Pages/TradingPairDetails.razor` - Integrated chart
- `Components/_Imports.razor` - Added ApexCharts using
- `Components/App.razor` - Added ApexCharts scripts

## Usage Example

```razor
<PriceChart Symbol="BTCUSDT" InitialMinutes="120" />
```

Parameters:
- `Symbol`: Trading pair (required)
- `InitialMinutes`: Initial time range (default: 60)

Public Methods:
```csharp
await priceChart.AddRealtimeData(klineData);  // Add real-time data
```

## Future Enhancements

Possible improvements:
- Volume overlay chart
- Technical indicators (MA, RSI, MACD)
- Drawing tools (trend lines, fibonacci)
- Multiple chart types (candle, bar, area)
- Chart templates/presets
- Auto-scroll with real-time data
- Price alerts on chart
- Order placement from chart
- Multi-symbol comparison
- Save/load chart configurations

## Troubleshooting

### Chart Not Loading:
- Check browser console for JavaScript errors
- Verify ApexCharts scripts loaded in App.razor
- Check network tab for failed requests

### Data Not Updating:
- Verify WebSocket connection active
- Check BinanceFuturesService logs
- Confirm priceChart reference is not null

### Zoom/Pan Not Working:
- Ensure toolbar tools are enabled
- Check chart options initialization
- Verify ApexCharts version compatibility

## Browser Support

- Chrome: Full support
- Firefox: Full support
- Edge: Full support
- Safari: Full support
- Mobile: Touch gestures supported
