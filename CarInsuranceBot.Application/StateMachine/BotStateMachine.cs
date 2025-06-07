using CarInsuranceBot.Application.StateMachine.States;
using CarInsuranceBot.Core.Entities;
using CarInsuranceBot.Core.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CarInsuranceBot.Application.StateMachine
{
    public class BotStateMachine
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BotStateMachine> _logger;
        private readonly Dictionary<ConversationState, IState> _states;
        private readonly IEnumerable<IGlobalCommandHandler> _globalCommandHandlers;

        public BotStateMachine(IServiceProvider serviceProvider, ILogger<BotStateMachine> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _states = InitializeStates();

            // Получаем все зарегистрированные глобальные хендлеры команд из DI
            _globalCommandHandlers = _serviceProvider.GetServices<IGlobalCommandHandler>();
        }

        private Dictionary<ConversationState, IState> InitializeStates()
        {
            return new Dictionary<ConversationState, IState>
            {
                { ConversationState.Start, _serviceProvider.GetRequiredService<StartState>() },
                { ConversationState.WaitingPassport, _serviceProvider.GetRequiredService<WaitingPassportState>() },
                { ConversationState.WaitingCarDoc, _serviceProvider.GetRequiredService<WaitingCarDocState>() },
                { ConversationState.WaitingConfirmation, _serviceProvider.GetRequiredService<ConfirmationState>() },
                { ConversationState.PriceConfirmation, _serviceProvider.GetRequiredService<PriceConfirmationState>() },
                { ConversationState.Completed, _serviceProvider.GetRequiredService<CompletedState>() }
            };
        }

        public async Task<ConversationState> ProcessUpdateAsync(UserSession session, Update update, CancellationToken cancellationToken = default)
        {
            try
            {
                // 1. Сначала пробуем обработать глобальные команды
                foreach (var handler in _globalCommandHandlers)
                {
                    var (handled, newState) = await handler.HandleCommandAsync(session, update, cancellationToken);
                    if (handled)
                    {
                        _logger.LogInformation("Global command handled by {Handler} for chat {ChatId}, switching state to {NewState}",
                            handler.GetType().Name, session.ChatId, newState);
                        return newState;
                    }
                }

                // 2. Если глобальная команда не сработала, обрабатываем текущее состояние
                if (!_states.TryGetValue(session.CurrentState, out var state))
                {
                    _logger.LogWarning("Unknown state {State} for chat {ChatId}", session.CurrentState, session.ChatId);
                    return ConversationState.Error;
                }

                var nextState = await state.HandleAsync(session, update, cancellationToken);

                _logger.LogInformation("State transition for chat {ChatId}: {CurrentState} -> {NextState}",
                    session.ChatId, session.CurrentState, nextState);

                return nextState;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing update for chat {ChatId} in state {State}",
                    session.ChatId, session.CurrentState);
                return ConversationState.Error;
            }
        }
    }
}
