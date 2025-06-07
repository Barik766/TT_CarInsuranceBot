using CarInsuranceBot.Core.Entities;
using CarInsuranceBot.Core.Enums;
using CarInsuranceBot.Core.Interfaces.Services;
using CarInsuranceBot.Core.Interfaces.Repositories;
using CarInsuranceBot.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarInsuranceBot.Infrastructure.Services
{
    public class StateManagerService : IStateManagerService
    {
        private readonly ISessionRepository _sessionRepository;
        private readonly ILogger<StateManagerService> _logger;

        public StateManagerService(ISessionRepository sessionRepository, ILogger<StateManagerService> logger)
        {
            _sessionRepository = sessionRepository;
            _logger = logger;
        }

        public async Task<ConversationState> GetUserStateAsync(long chatId, CancellationToken cancellationToken = default)
        {
            try
            {
                var session = await _sessionRepository.GetByChatIdAsync(chatId, cancellationToken);
                return session?.CurrentState ?? ConversationState.Start;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user state for chat {ChatId}", chatId);
                return ConversationState.Start;
            }
        }

        public async Task SetUserStateAsync(long chatId, ConversationState state, CancellationToken cancellationToken = default)
        {
            try
            {
                var session = await _sessionRepository.GetByChatIdAsync(chatId, cancellationToken);

                if (session == null)
                {
                    session = new UserSession
                    {
                        ChatId = chatId,
                        CurrentState = state,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _sessionRepository.CreateAsync(session, cancellationToken);
                }
                else
                {
                    session.CurrentState = state;
                    session.UpdatedAt = DateTime.UtcNow;
                    await _sessionRepository.UpdateAsync(session, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set user state {State} for chat {ChatId}", state, chatId);
                throw;
            }
        }

        public async Task<UserSession> GetSessionAsync(long chatId, CancellationToken cancellationToken = default)
        {
            try
            {
                var session = await _sessionRepository.GetByChatIdAsync(chatId, cancellationToken);

                if (session == null)
                {
                    session = new UserSession
                    {
                        ChatId = chatId,
                        CurrentState = ConversationState.Start,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _sessionRepository.CreateAsync(session, cancellationToken);
                }

                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get session for chat {ChatId}", chatId);
                throw;
            }
        }

        public async Task UpdateSessionAsync(UserSession session, CancellationToken cancellationToken = default)
        {
            try
            {
                session.UpdatedAt = DateTime.UtcNow;
                await _sessionRepository.UpdateAsync(session, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update session for chat {ChatId}", session.ChatId);
                throw;
            }
        }

        public async Task ClearSessionAsync(long chatId, CancellationToken cancellationToken = default)
        {
            var session = await _sessionRepository.GetByChatIdAsync(chatId, cancellationToken);
            if (session != null)
            {
                session.CurrentState = ConversationState.Start;
                session.PassportData = null;

                session.CarDocFrontFileId = null;
                session.CarDocBackFileId = null;

                session.ExtractedPassportData = null;
                session.ExtractedCarDocFront = null;
                session.ExtractedCarDocBack = null;

                session.IsDataConfirmed = false;
                session.IsPriceConfirmed = false;
                session.PolicyNumber = null;

                session.AdditionalData.Clear();
                session.UpdatedAt = DateTime.UtcNow;

                await _sessionRepository.UpdateAsync(session, cancellationToken);
            }

            await SetUserStateAsync(chatId, ConversationState.Start, cancellationToken);
        }

    }
}
