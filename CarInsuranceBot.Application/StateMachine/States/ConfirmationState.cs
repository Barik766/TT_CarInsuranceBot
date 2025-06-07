using CarInsuranceBot.Core.Entities;
using CarInsuranceBot.Core.Enums;
using CarInsuranceBot.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Telegram.Bot.Types;

namespace CarInsuranceBot.Application.StateMachine.States
{
    public class ConfirmationState : IState
    {
        private readonly ITelegramService _telegramService;
        private readonly ILogger<ConfirmationState> _logger;
        private readonly IConfiguration _configuration;

        public ConversationState StateType => ConversationState.WaitingConfirmation;

        public ConfirmationState(
            ITelegramService telegramService,
            ILogger<ConfirmationState> logger,
            IConfiguration configuration)
        {
            _telegramService = telegramService;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<ConversationState> HandleAsync(UserSession session, Update update, CancellationToken cancellationToken = default)
        {
            try
            {
                var message = update.Message?.Text?.Trim().ToLowerInvariant();

                if (message == "нет")
                {
                    await _telegramService.SendTextMessageAsync(session.ChatId,
                        "Хорошо, давайте начнём заново. Пожалуйста, загрузите фотографии документов.",
                        cancellationToken);

                    return ConversationState.WaitingPassport;
                }

                if (message != "да")
                {
                    await _telegramService.SendTextMessageAsync(session.ChatId,
                        "Пожалуйста, подтвердите данные, написав 'Да'. Если хотите начать заново — напишите 'Нет'.",
                        cancellationToken);

                    return ConversationState.WaitingConfirmation;
                }

                session.IsDataConfirmed = true;

                var price = _configuration.GetValue<decimal>("InsuranceSettings:Price");

                await _telegramService.SendTextMessageAsync(session.ChatId,
                    $"Отлично! Стоимость страховки: {price}$.\n Вы согласны? Подтвердите покупку, написав 'Подтверждаю'.",
                    cancellationToken);

                return ConversationState.PriceConfirmation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ConfirmationState for chat {ChatId}", session.ChatId);
                return ConversationState.Error;
            }
        }
    }
}
