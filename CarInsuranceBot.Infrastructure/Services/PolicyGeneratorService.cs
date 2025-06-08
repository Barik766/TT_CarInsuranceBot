using CarInsuranceBot.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace CarInsuranceBot.Infrastructure.Services
{
    public class PolicyGeneratorService : IPolicyGeneratorService
    {
        private readonly IOpenAIService _openAIService;
        private readonly ILogger<PolicyGeneratorService> _logger;

        public PolicyGeneratorService(IOpenAIService openAIService, ILogger<PolicyGeneratorService> logger)
        {
            _openAIService = openAIService;
            _logger = logger;
        }

        public async Task<string> GeneratePolicyAsync(string passportData, string carData, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(passportData) || string.IsNullOrWhiteSpace(carData))
            {
                _logger.LogWarning("Missing passport or car data for policy generation.");
                return "Insufficient data to generate a policy.";
            }

            try
            {
                var policyText = await _openAIService.GeneratePolicyContentAsync(passportData, carData, cancellationToken);
                return policyText;
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "Error generating policy content.");
                return "An error occurred while generating the policy.";
            }
        }
    }
}
