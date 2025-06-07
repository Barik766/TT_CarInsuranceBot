using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace CarInsuranceBot.Core.Entities
{
    public class ExtractedData
    {
        public int Id { get; set; }
        public string DocumentType { get; set; } = string.Empty;

        // Хранить сериализованный словарь в базе в этом поле
        public string FieldsJson { get; set; } = "{}";

        [NotMapped]
        public Dictionary<string, string> Fields
        {
            get
            {
                return string.IsNullOrEmpty(FieldsJson)
                    ? new Dictionary<string, string>()
                    : JsonSerializer.Deserialize<Dictionary<string, string>>(FieldsJson) ?? new Dictionary<string, string>();
            }
            set
            {
                FieldsJson = JsonSerializer.Serialize(value ?? new Dictionary<string, string>());
            }
        }

        public double Confidence { get; set; }
        public string RawData { get; set; } = string.Empty;
    }
}
