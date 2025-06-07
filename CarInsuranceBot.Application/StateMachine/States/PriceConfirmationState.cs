using CarInsuranceBot.Core.Entities;
using CarInsuranceBot.Core.Enums;
using CarInsuranceBot.Core.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CarInsuranceBot.Application.StateMachine.States
{
    public class PriceConfirmationState : IState
    {
        private readonly ITelegramService _telegramService;
        private readonly ILogger<PriceConfirmationState> _logger;

        public ConversationState StateType => ConversationState.PriceConfirmation;

        public PriceConfirmationState(ITelegramService telegramService, ILogger<PriceConfirmationState> logger)
        {
            _telegramService = telegramService;
            _logger = logger;
        }

        public async Task<ConversationState> HandleAsync(UserSession session, Update update, CancellationToken cancellationToken = default)
        {
            try
            {
                var message = update.Message?.Text?.ToLowerInvariant();

                if (message != "подтверждаю")
                {
                    await _telegramService.SendTextMessageAsync(session.ChatId, "Для продолжения напишите 'Подтверждаю'.", cancellationToken);
                    return ConversationState.PriceConfirmation;
                }

                session.IsPriceConfirmed = true;
                session.PolicyNumber = $"POL-{Guid.NewGuid().ToString()[..8].ToUpper()}";

                await _telegramService.SendTextMessageAsync(session.ChatId,
                    $"Поздравляем! Ваш полис оформлен. Номер полиса: {session.PolicyNumber}",
                    cancellationToken);
                return ConversationState.Completed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PriceConfirmationState for chat {ChatId}", session.ChatId);
                return ConversationState.Error;
            }
        }
    }

}
