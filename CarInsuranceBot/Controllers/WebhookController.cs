using CarInsuranceBot.Application.StateMachine;
using CarInsuranceBot.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using Telegram.Bot.Types;

namespace CarInsuranceBot.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WebhookController : ControllerBase
    {
        private readonly IStateManagerService _stateManager;
        private readonly BotStateMachine _stateMachine;
        private readonly ILogger<WebhookController> _logger;
        private readonly ITelegramService _telegramService;

        public WebhookController(
            IStateManagerService stateManager,
            BotStateMachine stateMachine,
            ITelegramService telegramService,
            ILogger<WebhookController> logger)
        {
            _stateManager = stateManager;
            _stateMachine = stateMachine;
            _telegramService = telegramService;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> ProcessUpdate([FromBody] Update update, CancellationToken cancellationToken = default)
        {
            try
            {
                if (update.Message?.Chat?.Id == null)
                {
                    return Ok();
                }

                var chatId = update.Message.Chat.Id;
                _logger.LogInformation("Processing update for chat {ChatId}", chatId);

                var session = await _stateManager.GetSessionAsync(chatId, cancellationToken);
                var messageText = update.Message?.Text?.Trim();

                
                if (messageText == "/start")
                {
                    var nextState = await _stateMachine.ProcessUpdateAsync(session, update, cancellationToken);
                    if (nextState != session.CurrentState)
                    {
                        await _stateManager.SetUserStateAsync(chatId, nextState, cancellationToken);
                    }

                    return Ok();
                }

                
                var nextRegularState = await _stateMachine.ProcessUpdateAsync(session, update, cancellationToken);
                if (nextRegularState != session.CurrentState)
                {
                    await _stateManager.SetUserStateAsync(chatId, nextRegularState, cancellationToken);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook update");
                return StatusCode(500);
            }
        }
    }
}
