using MarketParse.Components;
using MarketParse.Services;
using MarketParse.Models;

var builder = WebApplication.CreateBuilder(args);

// Add configuration files
builder.Configuration.AddJsonFile("pairs.json", optional: false, reloadOnChange: true);

// Configure TradingPairsConfig from JSON
builder.Services.Configure<TradingPairsConfig>(builder.Configuration);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register Binance Futures service
builder.Services.AddScoped<BinanceFuturesService>();

// Register Trading Pairs service
builder.Services.AddSingleton<TradingPairsService>();

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
