using CarInsuranceBot.Core.Entities;
using CarInsuranceBot.Core.Enums;
using CarInsuranceBot.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace CarInsuranceBot.Application.StateMachine.Transitions
{
    public class ResetCommandHandler : IGlobalCommandHandler
    {
        private readonly IStateManagerService _stateManager;
        private readonly ITelegramService _telegramService;
        private readonly ILogger<ResetCommandHandler> _logger;

        public ResetCommandHandler(IStateManagerService stateManager, ITelegramService telegramService, ILogger<ResetCommandHandler> logger)
        {
            _stateManager = stateManager;
            _telegramService = telegramService;
            _logger = logger;
        }

        public async Task<(bool Handled, ConversationState NewState)> HandleCommandAsync(UserSession session, Update update, CancellationToken cancellationToken)
        {
            if (update.Message?.Text?.Trim().ToLower() == "/reset" || session.CurrentState == ConversationState.WaitingConfirmation && update.Message?.Text?.Trim().ToLower() == "нет")
            {
                await _stateManager.ClearSessionAsync(session.ChatId, cancellationToken);

                // ОБНОВЛЯЕМ сессию в памяти:
                session.CurrentState = ConversationState.Start;
                // Сбрось здесь все дополнительные поля сессии, которые мешают повторному старту, если есть

                await _telegramService.SendTextMessageAsync(session.ChatId,
                    "✅ Ваше состояние успешно сброшено. Начинаем заново.",
                    cancellationToken);

                _logger.LogInformation("User session reset for chat {ChatId}", session.ChatId);
                return (true, ConversationState.Start);
            }

            return (false, session.CurrentState);
        }

    }

}
