using CarInsuranceBot.Core.Entities;
using CarInsuranceBot.Core.Enums;
using CarInsuranceBot.Core.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace CarInsuranceBot.Application.StateMachine.States
{
    public class CompletedState : IState
    {
        private readonly ITelegramService _telegramService;
        private readonly ILogger<CompletedState> _logger;

        public ConversationState StateType => ConversationState.Completed;

        public CompletedState(ITelegramService telegramService, ILogger<CompletedState> logger)
        {
            _telegramService = telegramService;
            _logger = logger;
        }

        public Task<ConversationState> HandleAsync(UserSession session, Update update, CancellationToken cancellationToken = default)
        {
            _telegramService.SendTextMessageAsync(session.ChatId,
                "✅ Ваш страховой полис уже оформлен. Если у вас есть вопросы, напишите нам!",
                cancellationToken);

            return Task.FromResult(ConversationState.Completed);
        }
    }

}
