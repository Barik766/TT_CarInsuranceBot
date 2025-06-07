using CarInsuranceBot.Core.Entities;
using CarInsuranceBot.Core.Enums;
using CarInsuranceBot.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace CarInsuranceBot.Application.StateMachine.States
{
    public class WaitingPassportState : IState
    {
        private readonly ITelegramService _telegramService;
        private readonly IMindeeService _mindeeService;
        private readonly ILogger<WaitingPassportState> _logger;

        public ConversationState StateType => ConversationState.WaitingPassport;

        public WaitingPassportState(
            ITelegramService telegramService,
            IMindeeService mindeeService,
            ILogger<WaitingPassportState> logger)
        {
            _telegramService = telegramService;
            _mindeeService = mindeeService;
            _logger = logger;
        }

        public async Task<ConversationState> HandleAsync(UserSession session, Update update, CancellationToken cancellationToken = default)
        {
            try
            {
                if (update.Message?.Photo == null)
                {
                    await _telegramService.SendTextMessageAsync(session.ChatId, "Пожалуйста, отправьте фото паспорта.", cancellationToken);
                    return ConversationState.WaitingPassport;
                }

                var fileId = update.Message.Photo.Last().FileId;
                var imageData = await _telegramService.DownloadFileAsync(fileId, cancellationToken);

                var extracted = await _mindeeService.ExtractPassportDataAsync(imageData, cancellationToken);

                session.PassportData = extracted.RawData;
                session.ExtractedPassportData = extracted;

                var message = $"📄 *Паспорт*\n" +
                              $"- Имя: {extracted.Fields.GetValueOrDefault("FirstName")} {extracted.Fields.GetValueOrDefault("LastName")}\n" +
                              $"- Паспорт: {extracted.Fields.GetValueOrDefault("PassportNumber")}\n" +
                              $"- Дата рождения: {extracted.Fields.GetValueOrDefault("BirthDate")}\n\n" +
                              $"Теперь отправьте фото техпаспорта автомобиля.";

                await _telegramService.SendTextMessageAsync(session.ChatId, message, cancellationToken);

                return ConversationState.WaitingCarDoc;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in WaitingPassportState for chat {ChatId}", session.ChatId);
                await _telegramService.SendTextMessageAsync(session.ChatId, "Произошла ошибка при обработке паспорта. Попробуйте ещё раз.", cancellationToken);
                return ConversationState.WaitingPassport;
            }
        }
    }
}
