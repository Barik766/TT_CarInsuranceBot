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
            // Проверяем команду "done" для завершения обработки
            if (update.Message?.Text?.ToLower() == "done")
            {
                return await ProcessDoneCommand(session, cancellationToken);
            }

            if (update.Message?.Photo == null)
            {
                await _telegramService.SendTextMessageAsync(session.ChatId,
                    "📋 Please send a photo of your vehicle registration document:\n\n" +
                    "• If your document has two sides - first send the front side (vehicle information), then the back side (owner information)\n" +
                    "• If your document is single-sided - send one photo\n\n" +
                    "After uploading all photos, type 'done' to continue.",
                    cancellationToken);
                return ConversationState.WaitingCarDoc;
            }

            var fileId = update.Message.Photo.Last().FileId;
            var imageData = await _telegramService.DownloadFileAsync(fileId, cancellationToken);

            if (session.CarDocFrontFileId == null)
            {
                // Обрабатываем первую сторону методом ExtractCarDocFrontAsync
                await ProcessFirstSide(session, fileId, imageData, cancellationToken);

                await _telegramService.SendTextMessageAsync(session.ChatId,
                    "✅ Front side of the technical passport has been processed.\n\n" +
                    "If your document has a back side with owner information - please upload it.\n" +
                    "If your document is single-sided - type 'done' to continue.",
                    cancellationToken);

                return ConversationState.WaitingCarDoc;
            }
            else
            {
                // Обрабатываем вторую сторону методом ExtractCarDocBackAsync
                await ProcessSecondSide(session, fileId, imageData, cancellationToken);

                await _telegramService.SendTextMessageAsync(session.ChatId,
                    "✅ Back side of the technical passport has been processed.\n\n" +
                    "Type 'done' to complete document processing.",
                    cancellationToken);

                return ConversationState.WaitingCarDoc;
            }
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

    private async Task ProcessFirstSide(UserSession session, string fileId, byte[] imageData, CancellationToken cancellationToken)
    {
        session.CarDocFrontFileId = fileId;

        // Используем ExtractCarDocFrontAsync для первой стороны/односторонного документа
        var extractedFront = await _mindeeService.ExtractCarDocFrontAsync(imageData, cancellationToken);
        session.ExtractedCarDocFront = extractedFront;

        await _stateManagerService.UpdateSessionAsync(session, cancellationToken);

        var frontText = FormatCarInfo(extractedFront?.Fields);
        await _telegramService.SendTextMessageAsync(session.ChatId,
            $"✅ Vehicle information extracted:\n\n🚗 *Vehicle details:*\n{frontText}",
            cancellationToken);
    }

    private async Task ProcessSecondSide(UserSession session, string fileId, byte[] imageData, CancellationToken cancellationToken)
    {
        session.CarDocBackFileId = fileId;

        // Используем ExtractCarDocBackAsync для второй стороны
        var extractedBack = await _mindeeService.ExtractCarDocBackAsync(imageData, cancellationToken);
        session.ExtractedCarDocBack = extractedBack;

        await _stateManagerService.UpdateSessionAsync(session, cancellationToken);

        var backText = FormatOwnerInfo(extractedBack?.Fields);
        await _telegramService.SendTextMessageAsync(session.ChatId,
            $"✅ Owner information extracted:\n\n👤 *Owner details:*\n{backText}",
            cancellationToken);
    }

    private async Task<ConversationState> ProcessDoneCommand(UserSession session, CancellationToken cancellationToken)
    {
        // Если есть только передняя сторона, пытаемся извлечь информацию о владельце
        if (session.CarDocFrontFileId != null && session.CarDocBackFileId == null)
        {
            try
            {
                // Получаем данные первого изображения
                var frontImageData = await _telegramService.DownloadFileAsync(session.CarDocFrontFileId, cancellationToken);

                // Используем ExtractCarDocBackAsync на том же изображении для извлечения информации о владельце
                var extractedOwner = await _mindeeService.ExtractCarDocBackAsync(frontImageData, cancellationToken);
                session.ExtractedCarDocBack = extractedOwner;

                await _stateManagerService.UpdateSessionAsync(session, cancellationToken);

                var ownerText = FormatOwnerInfo(extractedOwner?.Fields);
                await _telegramService.SendTextMessageAsync(session.ChatId,
                    $"✅ Owner information extracted from the same document:\n\n👤 *Owner details:*\n{ownerText}",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not extract owner information from single-sided document for chat {ChatId}", session.ChatId);
                // Продолжаем без информации о владельце
            }
        }

        // Формируем итоговую сводку
        var passportFields = session.ExtractedPassportData?.Fields;
        var frontFields = session.ExtractedCarDocFront?.Fields;
        var backFields = session.ExtractedCarDocBack?.Fields;

        _logger.LogInformation("CarDoc Front fields: {Fields}", JsonSerializer.Serialize(frontFields));
        _logger.LogInformation("CarDoc Back fields: {Fields}", JsonSerializer.Serialize(backFields));

        var summary = BuildFullSummary(passportFields, frontFields, backFields);
        await _telegramService.SendTextMessageAsync(session.ChatId, summary, cancellationToken);

        return ConversationState.WaitingConfirmation;
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
        var summary = "📋 *Document Summary:*\n\n";

        summary += "👤 *Passport Details:*\n";
        summary += FormatPassportInfo(passportFields) + "\n\n";

        summary += "🚗 *Vehicle Information:*\n";
        summary += FormatCarInfo(carFields) + "\n\n";

        summary += "📝 *Registration Details:*\n";
        summary += FormatOwnerInfo(ownerFields) + "\n\n";

        summary += "✅ *All documents have been processed. Do you confirm the entered information?*\n\n";
        summary += "Please confirm the details by writing 'Yes'.\n";
        summary += "Or write 'No' if you want to start over.";

        return summary;
    }
}