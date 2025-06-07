using System.Threading;
using System.Threading.Tasks;

namespace CarInsuranceBot.Core.Interfaces.Services
{
    public interface IPolicyGeneratorService
    {
        Task<string> GeneratePolicyAsync(string passportData, string carData, CancellationToken cancellationToken = default);
    }
}
