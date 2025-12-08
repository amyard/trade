using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;
using MarketParse.Models;

namespace MarketParse.Services;

public class TelegramBotService
{
    private readonly TelegramBotClient _botClient;
    private readonly TelegramConfig _config;
    private readonly ILogger<TelegramBotService> _logger;

    public TelegramBotService(IOptions<TelegramConfig> config, ILogger<TelegramBotService> logger)
    {
        _config = config.Value;
        _logger = logger;
        
        if (string.IsNullOrEmpty(_config.BotToken))
        {
            throw new ArgumentException("Telegram Bot Token is not configured");
        }
        
        _botClient = new TelegramBotClient(_config.BotToken);
    }

    /// <summary>
    /// Send a text message to the configured chat
    /// </summary>
    public async Task<bool> SendMessageAsync(string message)
    {
        try
        {
            if (_config.ChatId == 0)
            {
                _logger.LogWarning("Telegram Chat ID is not configured");
                return false;
            }

            await _botClient.SendMessage(
                chatId: _config.ChatId,
                text: message,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
            );

            _logger.LogInformation("Message sent to Telegram successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to Telegram");
            return false;
        }
    }

    /// <summary>
    /// Send trading pair information to Telegram
    /// </summary>
    public async Task<bool> SendTradingPairInfoAsync(string symbol, decimal price)
    {
        try
        {
            var message = $"<b>?? Trading Pair Alert</b>\n\n" +
                         $"<b>Symbol:</b> {symbol}\n" +
                         $"<b>Current Price:</b> ${price:F4}\n" +
                         $"<b>Time:</b> {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC";

            return await SendMessageAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error sending trading pair info for {symbol}");
            return false;
        }
    }

    /// <summary>
    /// Test connection to Telegram bot
    /// </summary>
    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var me = await _botClient.GetMe();
            _logger.LogInformation($"Telegram bot connected: @{me.Username}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Telegram bot");
            return false;
        }
    }
}
