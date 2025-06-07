using CarInsuranceBot.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using System.Net.Http.Json;

namespace CarInsuranceBot.Infrastructure.Services
{
    public class OpenAIService : IOpenAIService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenAIService> _logger;
        private readonly string _apiKey;
        private readonly string _openAiEndpoint;

        public OpenAIService(HttpClient httpClient, IConfiguration configuration, ILogger<OpenAIService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = configuration["OpenAI:ApiKey"] ?? throw new ArgumentNullException("OpenAI:ApiKey");
            _openAiEndpoint = configuration["OpenAI:Endpoint"] ?? "https://api.openai.com/v1/chat/completions";
            _logger.LogInformation("✅ OpenAI API key loaded: {Prefix}...", _apiKey.Substring(0, 5)); 
        }

        public async Task<string> GenerateResponseAsync(string prompt, string context = "", CancellationToken cancellationToken = default)
        {
            try
            {
                var messages = new List<object>();

                if (!string.IsNullOrWhiteSpace(context))
                    messages.Add(new { role = "system", content = context });

                messages.Add(new { role = "user", content = prompt });

                var requestBody = new
                {
                    model = "gpt-3.5-turbo",
                    messages,
                    max_tokens = 300,
                    temperature = 0.7
                };

                var request = new HttpRequestMessage(HttpMethod.Post, _openAiEndpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                request.Content = JsonContent.Create(requestBody);

                var response = await _httpClient.SendAsync(request, cancellationToken);

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    _logger.LogWarning("⚠️ OpenAI API rate limit exceeded (429).");
                    return "Слишком много запросов. Подождите немного и попробуйте снова.";
                }

                response.EnsureSuccessStatusCode();

                using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                using var jsonDoc = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);

                var choice = jsonDoc.RootElement.GetProperty("choices")[0];
                var message = choice.GetProperty("message").GetProperty("content").GetString();

                return message ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Ошибка при генерации ответа OpenAI.");
                return "Извините, возникла ошибка при обработке запроса.";
            }
        }

        public async Task<string> GeneratePolicyContentAsync(string passportData, string carData, CancellationToken cancellationToken = default)
        {
            var prompt = new StringBuilder();
            prompt.AppendLine("Пожалуйста, сгенерируй текст полиса автострахования на основе следующих данных:");
            prompt.AppendLine($"Паспортные данные: {passportData}");
            prompt.AppendLine($"Данные автомобиля: {carData}");
            prompt.AppendLine("Сделай текст формальным и профессиональным.");

            return await GenerateResponseAsync(prompt.ToString(), cancellationToken: cancellationToken);
        }
    }
}
