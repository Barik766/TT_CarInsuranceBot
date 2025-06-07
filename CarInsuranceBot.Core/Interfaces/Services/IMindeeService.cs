using CarInsuranceBot.Core.Entities;
using System.Threading;
using System.Threading.Tasks;

public interface IMindeeService
{
    Task<ExtractedData> ExtractPassportDataAsync(byte[] imageData, CancellationToken cancellationToken = default);
    Task<ExtractedData> ExtractCarDocFrontAsync(byte[] imageData, CancellationToken cancellationToken = default);
    Task<ExtractedData> ExtractCarDocBackAsync(byte[] imageData, CancellationToken cancellationToken = default);
}
