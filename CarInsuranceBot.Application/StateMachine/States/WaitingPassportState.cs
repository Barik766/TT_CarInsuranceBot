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

                var message = $"📄 *Passport*\n" +
                              $"- Name: {extracted.Fields.GetValueOrDefault("FirstName")} {extracted.Fields.GetValueOrDefault("LastName")}\n" +
                              $"- Passport: {extracted.Fields.GetValueOrDefault("PassportNumber")}\n" +
                              $"- Date of birth: {extracted.Fields.GetValueOrDefault("BirthDate")}\n\n" +
                              $"Now send a photo of the vehicle registration document (car type/manufacturer information side).";

                await _telegramService.SendTextMessageAsync(session.ChatId, message, cancellationToken);

                return ConversationState.WaitingCarDoc;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in WaitingPassportState for chat {ChatId}", session.ChatId);
                await _telegramService.SendTextMessageAsync(session.ChatId, "An error occurred while processing your passport. Please try again.", cancellationToken);
                return ConversationState.WaitingPassport;
            }
        }
    }
}
