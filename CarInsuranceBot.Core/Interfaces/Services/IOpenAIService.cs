using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarInsuranceBot.Core.Interfaces.Services
{
    public interface IOpenAIService
    {
        Task<string> GenerateResponseAsync(string prompt, string context = "", CancellationToken cancellationToken = default);
    }
}
