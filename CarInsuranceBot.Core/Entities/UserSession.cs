using CarInsuranceBot.Core.Entities;
using CarInsuranceBot.Core.Enums;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

public class UserSession
{
    public long ChatId { get; set; }
    public ConversationState CurrentState { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string? PassportData { get; set; }
    public string? CarDocFrontFileId { get; set; }
    public string? CarDocBackFileId { get; set; }

    public string? ExtractedPassportDataJson { get; set; }
    public string? ExtractedCarDocFrontJson { get; set; }
    public string? ExtractedCarDocBackJson { get; set; }

    [NotMapped]
    public ExtractedData? ExtractedPassportData
    {
        get => string.IsNullOrEmpty(ExtractedPassportDataJson) ? null : JsonSerializer.Deserialize<ExtractedData>(ExtractedPassportDataJson);
        set => ExtractedPassportDataJson = JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public ExtractedData? ExtractedCarDocFront
    {
        get => string.IsNullOrEmpty(ExtractedCarDocFrontJson) ? null : JsonSerializer.Deserialize<ExtractedData>(ExtractedCarDocFrontJson);
        set => ExtractedCarDocFrontJson = JsonSerializer.Serialize(value);
    }

    [NotMapped]
    public ExtractedData? ExtractedCarDocBack
    {
        get => string.IsNullOrEmpty(ExtractedCarDocBackJson) ? null : JsonSerializer.Deserialize<ExtractedData>(ExtractedCarDocBackJson);
        set => ExtractedCarDocBackJson = JsonSerializer.Serialize(value);
    }

    public bool IsDataConfirmed { get; set; }
    public bool IsPriceConfirmed { get; set; }
    public string? PolicyNumber { get; set; }

    public Dictionary<string, object> AdditionalData { get; set; } = new();
}
