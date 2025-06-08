using CarInsuranceBot.Core.Entities;
using CarInsuranceBot.Core.Enums;
using CarInsuranceBot.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace CarInsuranceBot.Application.StateMachine.States
{
    public class StartState : IState
    {
        private readonly ITelegramService _telegramService;
        private readonly IOpenAIService _openAIService;
        private readonly ILogger<StartState> _logger;

        public ConversationState StateType => ConversationState.Start;

        public StartState(ITelegramService telegramService, IOpenAIService openAIService, ILogger<StartState> logger)
        {
            _telegramService = telegramService;
            _openAIService = openAIService;
            _logger = logger;
        }

        public async Task<ConversationState> HandleAsync(UserSession session, Update update, CancellationToken cancellationToken = default)
        {
            try
            {

                var welcomeMessage = await _openAIService.GenerateResponseAsync(
                    "Generate a friendly welcome message for a car insurance bot. Keep it professional but warm.",
                    cancellationToken: cancellationToken);

                var fullMessage = $"{welcomeMessage}\n\n" +
                                  "🚗 Welcome to our car insurance service!\n\n" +
                                  "To get a policy, I'll need:\n" +
                                  "📋 Photo of your passport\n" +
                                  "🚙 Photo of the vehicle registration document\n\n" +
                                  "Please send a photo of your passport.";

                await _telegramService.SendTextMessageAsync(session.ChatId, fullMessage, cancellationToken);

                return ConversationState.WaitingPassport;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in StartState for chat {ChatId}", session.ChatId);
                return ConversationState.Error;
            }
        }
    }
}
