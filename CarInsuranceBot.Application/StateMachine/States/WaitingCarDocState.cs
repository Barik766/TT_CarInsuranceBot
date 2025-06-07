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
                    "Пожалуйста, отправьте фото техпаспорта (сначала переднюю сторону с информацией о машине, затем заднюю с информацией о владельце).",
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
                    $"✅ Передняя сторона техпаспорта обработана:\n\n🚗 *Информация о транспортном средстве:*\n{frontText}\n\n📋 Теперь отправьте заднюю сторону с информацией о владельце.",
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
            _logger.LogError(ex, "Ошибка в обработке техпаспорта для чата {ChatId}", session.ChatId);
            await _telegramService.SendTextMessageAsync(session.ChatId,
                "❌ Произошла ошибка при обработке документа. Попробуйте отправить фото ещё раз.",
                cancellationToken);
            return ConversationState.WaitingCarDoc;
        }
    }

    private string FormatCarInfo(Dictionary<string, string>? fields)
    {
        if (fields == null || fields.Count == 0)
            return "❌ Данные о транспортном средстве не распознаны.";

        return string.Join("\n", fields.Select(f => $"• {f.Key}: {f.Value}"));
    }

    private string FormatOwnerInfo(Dictionary<string, string>? fields)
    {
        if (fields == null || fields.Count == 0)
            return "❌ Данные о владельце не распознаны.";

        return string.Join("\n", fields.Select(f => $"• {f.Key}: {f.Value}"));
    }

    private string FormatPassportInfo(Dictionary<string, string>? fields)
    {
        if (fields == null || fields.Count == 0)
            return "❌ Данные паспорта не найдены.";

        return string.Join("\n", fields.Select(f => $"• {f.Key}: {f.Value}"));
    }

    private string BuildFullSummary(Dictionary<string, string>? passportFields,
                                  Dictionary<string, string>? carFields,
                                  Dictionary<string, string>? ownerFields)
    {
        var summary = "📋 *Сводная информация по документам:*\n\n";

        // Информация из паспорта
        summary += "👤 *Паспортные данные:*\n";
        summary += FormatPassportInfo(passportFields) + "\n\n";

        // Информация о транспортном средстве
        summary += "🚗 *Транспортное средство:*\n";
        summary += FormatCarInfo(carFields) + "\n\n";

        // Информация о владельце ТС
        summary += "📝 *Регистрационные данные:*\n";
        summary += FormatOwnerInfo(ownerFields) + "\n\n";

        summary += "✅ *Все документы обработаны. Подтверждаете введенную информацию?*\n\n";
        summary += "Пожалуйста, подтвердите данные, написав 'Да'.";

        return summary;
    }
}