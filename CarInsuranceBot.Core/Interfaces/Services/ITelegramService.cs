using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CarInsuranceBot.Core.Interfaces.Services
{
    public interface ITelegramService
    {
        Task SendTextMessageAsync(long chatId, string text, CancellationToken cancellationToken = default);
        Task SendDocumentAsync(long chatId, Stream document, string fileName, string? caption = null, CancellationToken cancellationToken = default);
        Task<byte[]> DownloadFileAsync(string fileId, CancellationToken cancellationToken = default);
        Task SetWebhookAsync(string webhookUrl, CancellationToken cancellationToken = default);
    }
}
