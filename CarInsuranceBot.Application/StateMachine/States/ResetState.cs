using CarInsuranceBot.Core.Entities;
using CarInsuranceBot.Core.Enums;
using CarInsuranceBot.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using System.Threading;
using System.Threading.Tasks;

namespace CarInsuranceBot.Application.StateMachine.States
{
    public class ResetState : IState
    {
        private readonly ITelegramService _telegramService;
        private readonly IStateManagerService _stateManager;
        private readonly ILogger<ResetState> _logger;

        public ConversationState StateType => ConversationState.Reset;

        public ResetState(ITelegramService telegramService, IStateManagerService stateManager, ILogger<ResetState> logger)
        {
            _telegramService = telegramService;
            _stateManager = stateManager;
            _logger = logger;
        }

        public async Task<ConversationState> HandleAsync(UserSession session, Update update, CancellationToken cancellationToken = default)
        {
            try
            {
                await _stateManager.ClearSessionAsync(session.ChatId, cancellationToken);

                await _telegramService.SendTextMessageAsync(session.ChatId,
                    "✅ Ваша сессия сброшена. Для нового начала введите /start.",
                    cancellationToken);

                _logger.LogInformation("User session has been reset for chat {ChatId}", session.ChatId);

                // После сброса пользователь должен вручную отправить /start
                return ConversationState.Reset;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting session for chat {ChatId}", session.ChatId);
                return session.CurrentState;
            }
        }
    }
}
