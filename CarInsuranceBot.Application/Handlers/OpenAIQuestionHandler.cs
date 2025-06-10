using CarInsuranceBot.Core.Entities;
using CarInsuranceBot.Core.Enums;
using CarInsuranceBot.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using System.Text;

namespace CarInsuranceBot.Application.StateMachine.Transitions
{
    public class OpenAIQuestionHandler : IGlobalCommandHandler
    {
        private readonly IOpenAIService _openAIService;
        private readonly ITelegramService _telegramService;
        private readonly ILogger<OpenAIQuestionHandler> _logger;

        private static readonly HashSet<string> IgnoredMessages = new(StringComparer.OrdinalIgnoreCase)
        {
            "yes", "no", "done"
        };

        public OpenAIQuestionHandler(
            IOpenAIService openAIService,
            ITelegramService telegramService,
            ILogger<OpenAIQuestionHandler> logger)
        {
            _openAIService = openAIService;
            _telegramService = telegramService;
            _logger = logger;
        }

        public async Task<(bool Handled, ConversationState NewState)> HandleCommandAsync(
            UserSession session,
            Update update,
            CancellationToken cancellationToken)
        {
            var messageText = update.Message?.Text?.Trim();

            // Игнорируем null/empty сообщения
            if (string.IsNullOrWhiteSpace(messageText))
            {
                return (false, session.CurrentState);
            }

            // Игнорируем команды
            if (messageText.StartsWith("/"))
            {
                return (false, session.CurrentState);
            }

            // Игнорируем файлы и фото
            if (update.Message?.Document != null || update.Message?.Photo != null)
            {
                return (false, session.CurrentState);
            }

            // Игнорируем специфичные ответы
            if (IgnoredMessages.Contains(messageText))
            {
                return (false, session.CurrentState);
            }

            // Обрабатываем все остальные текстовые сообщения
            try
            {
                var context = BuildContextForCurrentState(session.CurrentState);
                var prompt = BuildPromptForQuestion(messageText, session.CurrentState);

                var aiResponse = await _openAIService.GenerateResponseAsync(prompt, context, cancellationToken);

                await _telegramService.SendTextMessageAsync(
                    session.ChatId,
                    aiResponse,
                    cancellationToken);

                _logger.LogInformation("OpenAI question handled for chat {ChatId} in state {State}",
                    session.ChatId, session.CurrentState);

                return (true, session.CurrentState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling OpenAI question for chat {ChatId}", session.ChatId);

                await _telegramService.SendTextMessageAsync(
                    session.ChatId,
                    "Sorry, I can't process your question right now. Please follow the bot instructions.",
                    cancellationToken);

                return (true, session.CurrentState);
            }
        }

        private string BuildContextForCurrentState(ConversationState state)
        {
            var context = new StringBuilder();
            context.AppendLine("You are a helpful assistant for a car insurance bot.");
            context.AppendLine("Answer briefly, friendly and to the point.");
            context.AppendLine("Always guide the user to complete the current step of the process.");
            context.AppendLine("Keep responses under 100 words.");

            switch (state)
            {
                case ConversationState.Start:
                case ConversationState.WaitingPassport:
                    context.AppendLine("Current step: waiting for passport data upload.");
                    context.AppendLine("After answering the question, gently remind the user to send their passport photo.");
                    break;

                case ConversationState.WaitingCarDoc:
                    context.AppendLine("Current step: waiting for car document upload.");
                    context.AppendLine("After answering the question, gently remind the user to send their car documents.");
                    break;

                case ConversationState.WaitingConfirmation:
                    context.AppendLine("Current step: waiting for user to confirm their data.");
                    context.AppendLine("After answering the question, remind the user to confirm or correct their data.");
                    break;

                case ConversationState.PriceConfirmation:
                    context.AppendLine("Current step: waiting for price confirmation and policy creation.");
                    context.AppendLine("After answering the question, remind the user to make a decision about the policy.");
                    break;

                default:
                    context.AppendLine("Help the user with their question and guide them to start the process.");
                    break;
            }

            return context.ToString();
        }

        private string BuildPromptForQuestion(string question, ConversationState state)
        {
            var prompt = new StringBuilder();
            prompt.AppendLine($"User asks: \"{question}\"");
            prompt.AppendLine();
            prompt.AppendLine("Response requirements:");
            prompt.AppendLine("1. Answer the question briefly and clearly");
            prompt.AppendLine("2. Explain why the current step is needed (if relevant)");
            prompt.AppendLine("3. Gently guide to complete the current action");
            prompt.AppendLine("4. Use friendly tone");
            prompt.AppendLine("5. Maximum 2-3 sentences");

            switch (state)
            {
                case ConversationState.Start:
                case ConversationState.WaitingPassport:
                    prompt.AppendLine("6. End with a phrase about needing to send passport photo");
                    break;

                case ConversationState.WaitingCarDoc:
                    prompt.AppendLine("6. End with a phrase about needing to send car documents");
                    break;

                case ConversationState.WaitingConfirmation:
                    prompt.AppendLine("6. End with a phrase about needing to confirm the data");
                    break;

                case ConversationState.PriceConfirmation:
                    prompt.AppendLine("6. End with a phrase about needing to make a decision about the policy");
                    break;
            }

            return prompt.ToString();
        }
    }
}