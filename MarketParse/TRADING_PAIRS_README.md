# Trading Pairs Feature

## Overview
This feature allows users to browse a list of cryptocurrency trading pairs and view detailed information about each pair including historical data and real-time WebSocket updates from Binance Futures.

## Files Created

### 1. **Models/TradingPair.cs**
Data model representing a trading pair with:
- Symbol (e.g., "BTCUSDT")
- BaseAsset (e.g., "BTC")
- QuoteAsset (e.g., "USDT")
- DisplayName (e.g., "BTC/USDT")

### 2. **Services/TradingPairsService.cs**
Service managing the list of trading pairs with methods:
- `GetAllTradingPairs()` - Returns all available pairs
- `GetTradingPairBySymbol(string)` - Get specific pair by symbol
- `SearchTradingPairs(string)` - Search pairs by query

**Available Trading Pairs:**
- BTC/USDT, ETH/USDT, SOL/USDT, BNB/USDT
- XRP/USDT, ADA/USDT, DOGE/USDT, MATIC/USDT
- DOT/USDT, LTC/USDT, AVAX/USDT, LINK/USDT
- ATOM/USDT, ETC/USDT, UNI/USDT, AIA/USDT
- SHIB/USDT, APT/USDT, ARB/USDT, OP/USDT

### 3. **Components/Pages/TradingPairs.razor**
Main page displaying all trading pairs in a grid layout with:
- Search functionality to filter pairs
- Interactive cards that navigate to detail page on click
- Responsive design (4 columns on large screens, 3 on medium, 1 on small)
- Hover effects for better UX

### 4. **Components/Pages/TradingPairDetails.razor**
Detail page for each trading pair with:
- Dynamic route parameter `/trading-pair/{Symbol}`
- "Back to Trading Pairs" button
- Historical data loading (last 120 minutes)
- WebSocket connection for real-time updates
- Same functionality as BinanceFutures page but for selected pair

## Navigation

Added "Trading Pairs" link to the navigation menu with coin icon.

## Usage Flow

1. User navigates to "Trading Pairs" page
2. User sees grid of all available trading pairs
3. User can search for specific pairs using the search box
4. User clicks on any trading pair card
5. User is redirected to the detail page for that pair
6. User can load historical data and connect to WebSocket for real-time updates
7. User can return to the list using "Back to Trading Pairs" button

## Features

### Trading Pairs List Page
- ? Grid layout with cards
- ? Search functionality
- ? Total pairs counter
- ? Hover effects
- ? Click to navigate

### Trading Pair Details Page
- ? Load historical data (120 minutes)
- ? WebSocket real-time updates
- ? Data table with sorting
- ? Latest update card
- ? Loading indicators
- ? Error handling
- ? Navigation back to list

## Technical Details

- **Framework**: Blazor Server (.NET 9)
- **Render Mode**: InteractiveServer
- **API**: Binance Futures (USD-M)
- **Data Source**: BinanceFuturesService
- **Routing**: Dynamic routing with route parameters
- **DI**: Singleton service for trading pairs

## Dependencies

- Binance.Net package (already installed)
- BinanceFuturesService (already created)
- Bootstrap CSS (for styling)
