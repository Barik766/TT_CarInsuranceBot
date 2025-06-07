using CarInsuranceBot.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot;

namespace CarInsuranceBot.Infrastructure.Services
{
    public class TelegramService : ITelegramService
    {
        private readonly TelegramBotClient _botClient;
        private readonly ILogger<TelegramService> _logger;

        public TelegramService(IConfiguration configuration, ILogger<TelegramService> logger)
        {
            var token = configuration["Telegram:BotToken"] ?? throw new ArgumentNullException("Telegram:BotToken");
            _botClient = new TelegramBotClient(token);
            _logger = logger;
        }

        public async Task SendTextMessageAsync(long chatId, string text, CancellationToken cancellationToken = default)
        {
            try
            {
                await _botClient.SendTextMessageAsync(chatId, text, cancellationToken: cancellationToken);
                _logger.LogDebug("Sent text message to chat {ChatId}", chatId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send text message to chat {ChatId}", chatId);
                throw;
            }
        }

        public async Task SendDocumentAsync(long chatId, Stream document, string fileName, string? caption = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var inputFile = InputFile.FromStream(document, fileName);
                await _botClient.SendDocumentAsync(chatId, inputFile, caption: caption, cancellationToken: cancellationToken);
                _logger.LogDebug("Sent document {FileName} to chat {ChatId}", fileName, chatId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send document {FileName} to chat {ChatId}", fileName, chatId);
                throw;
            }
        }

        public async Task<byte[]> DownloadFileAsync(string fileId, CancellationToken cancellationToken = default)
        {
            try
            {
                var file = await _botClient.GetFileAsync(fileId, cancellationToken);

                using var stream = new MemoryStream();
                await _botClient.DownloadFileAsync(file.FilePath!, stream, cancellationToken);

                return stream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download file {FileId}", fileId);
                throw;
            }
        }

        public async Task SetWebhookAsync(string webhookUrl, CancellationToken cancellationToken = default)
        {
            try
            {
                await _botClient.SetWebhookAsync(webhookUrl, cancellationToken: cancellationToken);
                _logger.LogInformation("Webhook set to {WebhookUrl}", webhookUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set webhook to {WebhookUrl}", webhookUrl);
                throw;
            }
        }
    }
}
