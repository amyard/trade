using MarketParse.Components;
using MarketParse.Services;
using MarketParse.Models;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add configuration files
builder.Configuration.AddJsonFile("pairs.json", optional: false, reloadOnChange: true);

// Configure TradingPairsConfig from JSON
builder.Services.Configure<TradingPairsConfig>(builder.Configuration);

// Configure TelegramConfig from appsettings
builder.Services.Configure<TelegramConfig>(builder.Configuration.GetSection("Telegram"));

// Configure RSI Strategy settings from appsettings
builder.Services.Configure<RSIStrategyConfig>(builder.Configuration.GetSection("RSIStrategy"));

// Configure Volume Filter settings from appsettings
builder.Services.Configure<VolumeFilterConfig>(builder.Configuration.GetSection("VolumeFilter"));

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add MudBlazor services
builder.Services.AddMudServices();

// Configure Blazor Server with detailed errors in development
builder.Services.AddServerSideBlazor()
    .AddCircuitOptions(options =>
    {
        if (builder.Environment.IsDevelopment())
        {
            options.DetailedErrors = true;
        }
    });

// Register Binance Futures service
builder.Services.AddScoped<BinanceFuturesService>();

// Register Trading Pairs service
builder.Services.AddSingleton<TradingPairsService>();

// Register Telegram Bot service
builder.Services.AddSingleton<TelegramBotService>();

// Register Volume Filter service
builder.Services.AddSingleton<VolumeFilterService>();

// Register Background Service for WebSocket monitoring
builder.Services.AddHostedService<BinanceWebSocketBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
