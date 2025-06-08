using CarInsuranceBot.Application.StateMachine.States;
using CarInsuranceBot.Core.Enums;
using CarInsuranceBot.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using JsonSerializer = System.Text.Json.JsonSerializer;

public class WaitingCarDocState : IState
{
    private readonly ITelegramService _telegramService;
    private readonly IMindeeService _mindeeService;
    private readonly ILogger<WaitingCarDocState> _logger;
    private readonly IStateManagerService _stateManagerService;

    public ConversationState StateType => ConversationState.WaitingCarDoc;

    public WaitingCarDocState(
        ITelegramService telegramService,
        IMindeeService mindeeService,
        IStateManagerService stateManagerService,
        ILogger<WaitingCarDocState> logger)
    {
        _telegramService = telegramService;
        _mindeeService = mindeeService;
        _stateManagerService = stateManagerService;
        _logger = logger;
    }

    public async Task<ConversationState> HandleAsync(UserSession session, Update update, CancellationToken cancellationToken = default)
    {
        try
        {
            if (update.Message?.Photo == null)
            {
                await _telegramService.SendTextMessageAsync(session.ChatId,
                    "Please send a photo of the vehicle registration document (first the front side with information about the vehicle, then the back side with information about the owner).",
                    cancellationToken);
                return ConversationState.WaitingCarDoc;
            }

            var fileId = update.Message.Photo.Last().FileId;
            var imageData = await _telegramService.DownloadFileAsync(fileId, cancellationToken);

            if (session.CarDocFrontFileId == null)
            {
                // Обрабатываем переднюю сторону (информация о машине)
                session.CarDocFrontFileId = fileId;
                var extractedFront = await _mindeeService.ExtractCarDocFrontAsync(imageData, cancellationToken);
                session.ExtractedCarDocFront = extractedFront;

                await _stateManagerService.UpdateSessionAsync(session, cancellationToken);

                var frontText = FormatCarInfo(extractedFront?.Fields);

                await _telegramService.SendTextMessageAsync(session.ChatId,
                    $"✅ The front side of the technical passport has been processed.:\n\n🚗 *Vehicle information:*\n{frontText}\n\n📋 Now send the back side with the owner's information..",
                    cancellationToken);

                return ConversationState.WaitingCarDoc;
            }

            // Обрабатываем заднюю сторону (информация о владельце)
            session.CarDocBackFileId = fileId;
            var extractedBack = await _mindeeService.ExtractCarDocBackAsync(imageData, cancellationToken);
            session.ExtractedCarDocBack = extractedBack;

            await _stateManagerService.UpdateSessionAsync(session, cancellationToken);

            var passportFields = session.ExtractedPassportData?.Fields;
            var frontFields = session.ExtractedCarDocFront?.Fields;
            var backFields = session.ExtractedCarDocBack?.Fields;

            _logger.LogInformation("CarDoc Front fields: {Fields}", JsonSerializer.Serialize(frontFields));
            _logger.LogInformation("CarDoc Back fields: {Fields}", JsonSerializer.Serialize(backFields));

            var summary = BuildFullSummary(passportFields, frontFields, backFields);

            await _telegramService.SendTextMessageAsync(session.ChatId, summary, cancellationToken);

            return ConversationState.WaitingConfirmation;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in processing the technical passport for chat {ChatId}", session.ChatId);
            await _telegramService.SendTextMessageAsync(session.ChatId,
                "❌ An error occurred while processing the document. Please try sending the photo again.",
                cancellationToken);
            return ConversationState.WaitingCarDoc;
        }
    }

    private string FormatCarInfo(Dictionary<string, string>? fields)
    {
        if (fields == null || fields.Count == 0)
            return "❌ Vehicle data not recognized.";

        return string.Join("\n", fields.Select(f => $"• {f.Key}: {f.Value}"));
    }

    private string FormatOwnerInfo(Dictionary<string, string>? fields)
    {
        if (fields == null || fields.Count == 0)
            return "❌ Owner information not recognized.";

        return string.Join("\n", fields.Select(f => $"• {f.Key}: {f.Value}"));
    }

    private string FormatPassportInfo(Dictionary<string, string>? fields)
    {
        if (fields == null || fields.Count == 0)
            return "❌ Passport details not found.";

        return string.Join("\n", fields.Select(f => $"• {f.Key}: {f.Value}"));
    }

    private string BuildFullSummary(Dictionary<string, string>? passportFields,
                                  Dictionary<string, string>? carFields,
                                  Dictionary<string, string>? ownerFields)
    {
        var summary = "📋 *Summary information on documents:*\n\n";

        summary += "👤 *Passport details:*\n";
        summary += FormatPassportInfo(passportFields) + "\n\n";

        summary += "🚗 *Vehicle:*\n";
        summary += FormatCarInfo(carFields) + "\n\n";

        summary += "📝 *Registration details:*\n";
        summary += FormatOwnerInfo(ownerFields) + "\n\n";

        summary += "✅ *All documents have been processed. Do you confirm the information you entered?*\n\n";
        summary += "Please confirm the details by writing 'Yes'.";

        return summary;
    }
}