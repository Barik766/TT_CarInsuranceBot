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

                if (message == "no")
                {
                    await _telegramService.SendTextMessageAsync(session.ChatId,
                        "Okay, let's start over. Please upload photos of your documents.",
                        cancellationToken);

                    return ConversationState.WaitingPassport;
                }

                if (message != "yes")
                {
                    await _telegramService.SendTextMessageAsync(session.ChatId,
                        "Please confirm the information by typing 'Yes'. If you want to start over, type 'No'.",
                        cancellationToken);

                    return ConversationState.WaitingConfirmation;
                }

                session.IsDataConfirmed = true;

                var price = _configuration.GetValue<decimal>("InsuranceSettings:Price");

                await _telegramService.SendTextMessageAsync(session.ChatId,
                    $"Great! Insurance cost: {price}$.\n Do you agree? Confirm your purchase by typing 'Confirm'.",
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
