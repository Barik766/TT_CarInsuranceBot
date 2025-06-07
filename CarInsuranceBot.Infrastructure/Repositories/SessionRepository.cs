using CarInsuranceBot.Core.Entities;
using CarInsuranceBot.Core.Interfaces.Repositories;
using CarInsuranceBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CarInsuranceBot.Infrastructure.Repositories
{
    public class SessionRepository : ISessionRepository
    {
        private readonly BotDbContext _dbContext;
        private readonly ILogger<SessionRepository> _logger;

        public SessionRepository(BotDbContext dbContext, ILogger<SessionRepository> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<UserSession?> GetByChatIdAsync(long chatId, CancellationToken cancellationToken = default)
        {
            try
            {
                return await _dbContext.UserSessions
                    .FirstOrDefaultAsync(s => s.ChatId == chatId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get UserSession for chatId {ChatId}", chatId);
                throw;
            }
        }

        public async Task CreateAsync(UserSession session, CancellationToken cancellationToken = default)
        {
            try
            {
                await _dbContext.UserSessions.AddAsync(session, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create UserSession for chatId {ChatId}", session.ChatId);
                throw;
            }
        }

        public async Task UpdateAsync(UserSession session, CancellationToken cancellationToken = default)
        {
            try
            {
                _dbContext.UserSessions.Update(session);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update UserSession for chatId {ChatId}", session.ChatId);
                throw;
            }
        }
    }
}
