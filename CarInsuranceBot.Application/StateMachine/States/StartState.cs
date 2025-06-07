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
                // Убрали проверку на повторный запуск /start

                var welcomeMessage = await _openAIService.GenerateResponseAsync(
                    "Generate a friendly welcome message for a car insurance bot. Keep it professional but warm.",
                    cancellationToken: cancellationToken);

                var fullMessage = $"{welcomeMessage}\n\n" +
                                  "🚗 Добро пожаловать в наш сервис автострахования!\n\n" +
                                  "Для оформления полиса мне потребуются:\n" +
                                  "📋 Фото вашего паспорта\n" +
                                  "🚙 Фото техпаспорта автомобиля\n\n" +
                                  "Пожалуйста, отправьте фото вашего паспорта.";

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
