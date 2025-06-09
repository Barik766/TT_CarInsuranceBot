using CarInsuranceBot.Core.Entities;
using CarInsuranceBot.Core.Enums;
using CarInsuranceBot.Core.Interfaces.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Text;

namespace CarInsuranceBot.Application.StateMachine.States
{
    public class PriceConfirmationState : IState
    {
        private readonly ITelegramService _telegramService;
        private readonly IPolicyGeneratorService _policyGeneratorService;
        private readonly ILogger<PriceConfirmationState> _logger;

        public ConversationState StateType => ConversationState.PriceConfirmation;

        public PriceConfirmationState(ITelegramService telegramService, IPolicyGeneratorService policyGeneratorService, ILogger<PriceConfirmationState> logger)
        {
            _telegramService = telegramService;
            _policyGeneratorService = policyGeneratorService;
            _logger = logger;
        }

        public async Task<ConversationState> HandleAsync(UserSession session, Update update, CancellationToken cancellationToken = default)
        {
            try
            {
                var message = update.Message?.Text?.ToLowerInvariant();

                if (message != "yes")
                {
                    await _telegramService.SendTextMessageAsync(session.ChatId, "To continue, type 'yes'.", cancellationToken);
                    return ConversationState.PriceConfirmation;
                }

                session.IsPriceConfirmed = true;
                session.PolicyNumber = $"POL-{Guid.NewGuid().ToString()[..8].ToUpper()}";

                // Generate policy using PolicyGeneratorService
                var policyContent = await _policyGeneratorService.GeneratePolicyAsync(
                    session.PassportData ?? "Passport data not available",
                    $"Front: {session.CarDocFrontFileId}, Back: {session.CarDocBackFileId}",
                    cancellationToken);

                // Create final message with policy information
                await _telegramService.SendTextMessageAsync(session.ChatId,
                    $"🎉 Congratulations! Your car insurance policy has been issued!\n" +
                    $"📋 Policy Number: {session.PolicyNumber}\n\n" +
                    $"📄 Policy Details:\n{policyContent}",
                    cancellationToken);

                return ConversationState.Completed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PriceConfirmationState for chat {ChatId}", session.ChatId);

                // Fallback message in case of error
                session.IsPriceConfirmed = true;
                session.PolicyNumber = $"POL-{Guid.NewGuid().ToString()[..8].ToUpper()}";

                await _telegramService.SendTextMessageAsync(session.ChatId,
                    $"Congratulations! Your policy has been issued. Policy number: {session.PolicyNumber}",
                    cancellationToken);

                return ConversationState.Completed;
            }
        }
    }
}