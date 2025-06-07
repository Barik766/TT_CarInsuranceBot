using CarInsuranceBot.Core.Entities;
using CarInsuranceBot.Core.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarInsuranceBot.Core.Interfaces.Services
{
    public interface IStateManagerService
    {
        Task<ConversationState> GetUserStateAsync(long chatId, CancellationToken cancellationToken = default);
        Task SetUserStateAsync(long chatId, ConversationState state, CancellationToken cancellationToken = default);
        Task<UserSession> GetSessionAsync(long chatId, CancellationToken cancellationToken = default);
        Task UpdateSessionAsync(UserSession session, CancellationToken cancellationToken = default);
        Task ClearSessionAsync(long chatId, CancellationToken cancellationToken = default);

    }
}
