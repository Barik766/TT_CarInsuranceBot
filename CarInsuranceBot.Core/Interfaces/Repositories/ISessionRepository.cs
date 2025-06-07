using CarInsuranceBot.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarInsuranceBot.Core.Interfaces.Repositories
{
    public interface ISessionRepository
    {
        Task<UserSession?> GetByChatIdAsync(long chatId, CancellationToken cancellationToken = default);
        Task CreateAsync(UserSession session, CancellationToken cancellationToken = default);
        Task UpdateAsync(UserSession session, CancellationToken cancellationToken = default);
    }
}
