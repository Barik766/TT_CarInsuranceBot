using CarInsuranceBot.Core.Entities;
using CarInsuranceBot.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;


namespace CarInsuranceBot.Application.StateMachine.States
{
    public interface IState
    {
        ConversationState StateType { get; }
        Task<ConversationState> HandleAsync(UserSession session, Update update, CancellationToken cancellationToken = default);
    }
}
