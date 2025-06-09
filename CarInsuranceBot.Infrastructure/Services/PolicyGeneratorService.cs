using CarInsuranceBot.Core.Interfaces.Services;
using Microsoft.Extensions.Logging;
using System.Text;
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
                var context = BuildPolicyGenerationContext();
                var prompt = BuildPolicyGenerationPrompt(passportData, carData);

                var policyText = await _openAIService.GenerateResponseAsync(prompt, context, cancellationToken);

                _logger.LogInformation("Policy generated successfully for passport data length: {PassportLength}, car data length: {CarLength}",
                    passportData.Length, carData.Length);

                return policyText;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating policy content.");
                return "An error occurred while generating the policy.";
            }
        }

        private string BuildPolicyGenerationContext()
        {
            var context = new StringBuilder();
            context.AppendLine("You are a professional insurance policy document generator.");
            context.AppendLine("Generate formal, comprehensive car insurance policy documents.");
            context.AppendLine("Use professional insurance terminology and standard policy structure.");
            context.AppendLine("Include all necessary legal disclaimers and coverage details.");
            context.AppendLine("Make the document look authentic and complete.");

            return context.ToString();
        }

        private string BuildPolicyGenerationPrompt(string passportData, string carData)
        {
            var prompt = new StringBuilder();
            prompt.AppendLine("Please generate a complete car insurance policy document based on the following data:");
            prompt.AppendLine();
            prompt.AppendLine($"Passport/Personal Data: {passportData}");
            prompt.AppendLine($"Vehicle Data: {carData}");
            prompt.AppendLine();
            prompt.AppendLine("Requirements:");
            prompt.AppendLine("- Make the text formal and professional");
            prompt.AppendLine("- Include policy terms, coverage details, and important information");
            prompt.AppendLine("- Use only the information provided - do not ask for additional data");
            prompt.AppendLine("- Fill all fields with appropriate data based on provided information");
            prompt.AppendLine("- The only field that should remain blank is the signature field");
            prompt.AppendLine("- Include policy number, effective dates, premium amount, and coverage limits");
            prompt.AppendLine("- Add standard insurance terms and conditions");

            return prompt.ToString();
        }
    }
}