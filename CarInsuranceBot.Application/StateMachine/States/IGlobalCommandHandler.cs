using CarInsuranceBot.Core.Entities;
using CarInsuranceBot.Core.Enums;
using Telegram.Bot.Types;

public interface IGlobalCommandHandler
{
    Task<(bool Handled, ConversationState NewState)> HandleCommandAsync(UserSession session, Update update, CancellationToken cancellationToken);
}
