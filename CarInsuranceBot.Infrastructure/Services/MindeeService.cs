using CarInsuranceBot.Core.Entities;
using CarInsuranceBot.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mindee;
using Mindee.Input;
using Mindee.Http;
using Mindee.Product.Generated;
using System.Text.Json;

namespace CarInsuranceBot.Infrastructure.Services
{
    public class MindeeService : IMindeeService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MindeeService> _logger;
        private readonly MindeeClient _mindeeClient;

        public MindeeService(IConfiguration configuration, ILogger<MindeeService> logger)
        {
            _configuration = configuration;
            _logger = logger;

            var apiKey = _configuration["Mindee:ApiKey"];
            _mindeeClient = new MindeeClient(apiKey);

            _logger.LogInformation("MindeeService created with official SDK");
        }

        public async Task<ExtractedData> ExtractPassportDataAsync(byte[] imageData, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("ExtractPassportDataAsync started");

            try
            {
                var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.jpg");
                await File.WriteAllBytesAsync(tempFilePath, imageData, cancellationToken);

                try
                {
                    var inputSource = new LocalInputSource(tempFilePath);

                    var endpoint = new CustomEndpoint(
                        endpointName: "passport",
                        accountName: "mindee",
                        version: "1"
                    );

                    var response = await _mindeeClient.ParseAsync<GeneratedV1>(inputSource, endpoint);
                    return ParsePassportResponse(response.Document.ToString(), "Passport");
                }
                finally
                {
                    if (File.Exists(tempFilePath))
                        File.Delete(tempFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting passport data");
                return new ExtractedData
                {
                    DocumentType = "Passport",
                    RawData = ex.Message,
                    Confidence = 0
                };
            }
        }



        public async Task<ExtractedData> ExtractCarDocFrontAsync(byte[] imageData, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("ExtractCarDocFrontAsync started");

            try
            {
                var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.jpg");
                await File.WriteAllBytesAsync(tempFilePath, imageData, cancellationToken);

                try
                {
                    var inputSource = new LocalInputSource(tempFilePath);
                    var endpoint = GetCustomEndpoint("CarDocFront");

                    _logger.LogInformation("Calling Mindee API for CarDocFront with endpoint: {EndpointName}", endpoint.EndpointName);
                    var response = await _mindeeClient.EnqueueAndParseAsync<GeneratedV1>(inputSource, endpoint);
                    _logger.LogInformation("CarDocFront API call successful");

                    return ParseCarFrontResponse(response.Document.ToString(), "CarDocFront");
                }
                finally
                {
                    if (File.Exists(tempFilePath))
                        File.Delete(tempFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting car document front data");
                return new ExtractedData
                {
                    DocumentType = "CarDocFront",
                    RawData = ex.Message,
                    Confidence = 0
                };
            }
        }

        public async Task<ExtractedData> ExtractCarDocBackAsync(byte[] imageData, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("ExtractCarDocBackAsync started");

            try
            {
                var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.jpg");
                await File.WriteAllBytesAsync(tempFilePath, imageData, cancellationToken);

                try
                {
                    var inputSource = new LocalInputSource(tempFilePath);
                    var endpoint = GetCustomEndpoint("CarDocBack");

                    _logger.LogInformation("Calling Mindee API for CarDocBack with endpoint: {EndpointName}", endpoint.EndpointName);
                    var response = await _mindeeClient.EnqueueAndParseAsync<GeneratedV1>(inputSource, endpoint);
                    _logger.LogInformation("CarDocBack API call successful");

                    return ParseCarBackResponse(response.Document.ToString(), "CarDocBack");
                }
                finally
                {
                    if (File.Exists(tempFilePath))
                        File.Delete(tempFilePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting car document back data");
                return new ExtractedData
                {
                    DocumentType = "CarDocBack",
                    RawData = ex.Message,
                    Confidence = 0
                };
            }
        }

        private CustomEndpoint GetCustomEndpoint(string sectionName)
        {
            var section = _configuration.GetSection($"Mindee:Endpoints:{sectionName}");
            var accountName = section["AccountName"];
            var endpointName = section["EndpointName"];
            var version = section["Version"];

            if (string.IsNullOrEmpty(accountName) || string.IsNullOrEmpty(endpointName) || string.IsNullOrEmpty(version))
            {
                throw new InvalidOperationException($"Missing configuration for Mindee endpoint '{sectionName}'.");
            }

            return new CustomEndpoint(endpointName, accountName, version);
        }

        private ExtractedData ParsePassportResponse(string documentString, string docType)
        {
            _logger.LogDebug("Parsing passport response for {DocType}", docType);

            try
            {
                var fields = ExtractFieldsFromResponse(documentString);

                _logger.LogInformation("Parsed passport fields: {Fields}",
                    string.Join(", ", fields.Select(f => $"{f.Key}={f.Value}")));

                var userFriendlyFields = new Dictionary<string, string>();

                if (fields.TryGetValue("given_names", out var firstName))
                    userFriendlyFields["FirstName"] = firstName;

                if (fields.TryGetValue("surname", out var lastName))
                    userFriendlyFields["LastName"] = lastName;

                if (fields.TryGetValue("id_number", out var passport))
                    userFriendlyFields["PassportNumber"] = passport;

                if (fields.TryGetValue("birth_date", out var birth))
                    userFriendlyFields["BirthDate"] = birth;

                return new ExtractedData
                {
                    DocumentType = docType,
                    Fields = userFriendlyFields,
                    Confidence = 1.0,
                    RawData = documentString
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing passport response");
                return new ExtractedData
                {
                    DocumentType = docType,
                    Fields = new Dictionary<string, string>(),
                    Confidence = 0,
                    RawData = documentString
                };
            }
        }

        private ExtractedData ParseCarFrontResponse(string documentString, string docType)
        {
            _logger.LogDebug("Parsing car front response for {DocType}", docType);

            try
            {
                var fields = ExtractFieldsFromResponse(documentString);
                var userFriendlyFields = new Dictionary<string, string>();

                foreach (var field in fields)
                {
                    switch (field.Key.ToLower())
                    {
                        case "manufacturer":
                            userFriendlyFields["Производитель"] = field.Value;
                            break;
                        case "model":
                            userFriendlyFields["Модель"] = field.Value;
                            break;
                        default:
                            userFriendlyFields[field.Key] = field.Value;
                            break;
                    }
                }

                _logger.LogInformation("Parsed {Count} car front fields", userFriendlyFields.Count);

                return new ExtractedData
                {
                    DocumentType = docType,
                    Fields = userFriendlyFields,
                    Confidence = 1.0,
                    RawData = documentString
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing car front response");
                return new ExtractedData
                {
                    DocumentType = docType,
                    Fields = new Dictionary<string, string>(),
                    Confidence = 0,
                    RawData = documentString
                };
            }
        }

        private ExtractedData ParseCarBackResponse(string documentString, string docType)
        {
            _logger.LogDebug("Parsing car back response for {DocType}", docType);

            try
            {
                var fields = ExtractFieldsFromResponse(documentString);
                var userFriendlyFields = new Dictionary<string, string>();

                foreach (var field in fields)
                {
                    switch (field.Key.ToLower())
                    {
                        case "registration_number":
                            userFriendlyFields["Registration number"] = field.Value;
                            break;
                        case "surname":
                            userFriendlyFields["Last name"] = field.Value;
                            break;
                        case "name":
                            userFriendlyFields["Name"] = field.Value;
                            break;
                        default:
                            userFriendlyFields[field.Key] = field.Value;
                            break;
                    }
                }

                _logger.LogInformation("Parsed {Count} car back fields", userFriendlyFields.Count);

                return new ExtractedData
                {
                    DocumentType = docType,
                    Fields = userFriendlyFields,
                    Confidence = 1.0,
                    RawData = documentString
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing car back response");
                return new ExtractedData
                {
                    DocumentType = docType,
                    Fields = new Dictionary<string, string>(),
                    Confidence = 0,
                    RawData = documentString
                };
            }
        }

        private Dictionary<string, string> ExtractFieldsFromResponse(string documentString)
        {
            var fields = new Dictionary<string, string>();

            try
            {
                _logger.LogInformation("RAW JSON RESPONSE:\n{Json}", documentString);

                if (documentString.TrimStart().StartsWith("{"))
                {
                    var jsonDoc = JsonDocument.Parse(documentString);
                    JsonElement prediction;

                    // Пробуем достать prediction
                    if (jsonDoc.RootElement.TryGetProperty("document", out var docElem) &&
                        docElem.TryGetProperty("inference", out var inferenceElem) &&
                        inferenceElem.TryGetProperty("prediction", out prediction))
                    {
                        _logger.LogInformation("Found prediction section");
                        ExtractFieldsFromJson(prediction, fields);
                    }
                    else
                    {
                        _logger.LogWarning("No prediction found, using full JSON root");
                        ExtractFieldsFromJson(jsonDoc.RootElement, fields);
                    }

                    _logger.LogInformation("Parsed {Count} fields from JSON", fields.Count);
                }
                else
                {
                    _logger.LogWarning("Not a JSON response, falling back to line-based parsing");
                    ParseStringResponse(documentString, fields);
                }
            }
            catch (JsonException je)
            {
                _logger.LogWarning(je, "JSON parsing failed. Fallback to line-based parse.");
                ParseStringResponse(documentString, fields);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error during response parsing.");
            }

            return fields;
        }


        private void ExtractFieldsFromPrediction(JsonElement predictionElement, Dictionary<string, string> fields)
        {
            foreach (var property in predictionElement.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.Object)
                {
                    if (property.Value.TryGetProperty("value", out var valueElement))
                    {
                        var value = valueElement.ValueKind == JsonValueKind.String
                            ? valueElement.GetString()
                            : valueElement.GetRawText().Trim('"');

                        if (!string.IsNullOrEmpty(value) && value != "null")
                        {
                            fields[property.Name] = value;
                            _logger.LogDebug("Extracted field: {Key} = {Value}", property.Name, value);
                        }
                    }
                }
                else if (property.Value.ValueKind == JsonValueKind.String)
                {
                    var value = property.Value.GetString();
                    if (!string.IsNullOrEmpty(value) && value != "null")
                    {
                        fields[property.Name] = value;
                        _logger.LogDebug("Extracted field: {Key} = {Value}", property.Name, value);
                    }
                }
                else if (property.Value.ValueKind == JsonValueKind.Number)
                {
                    var value = property.Value.ToString();
                    fields[property.Name] = value;
                    _logger.LogDebug("Extracted field: {Key} = {Value}", property.Name, value);
                }
            }
        }

        private void ExtractFieldsFromJson(JsonElement element, Dictionary<string, string> fields, string prefix = "")
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.Object:
                    foreach (var property in element.EnumerateObject())
                    {
                        var key = string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}.{property.Name}";
                        ExtractFieldsFromJson(property.Value, fields, key);
                    }
                    break;

                case JsonValueKind.Array:
                    int index = 0;
                    foreach (var item in element.EnumerateArray())
                    {
                        ExtractFieldsFromJson(item, fields, $"{prefix}[{index}]");
                        index++;
                    }
                    break;

                case JsonValueKind.String:
                    if (!string.IsNullOrWhiteSpace(element.GetString()))
                    {
                        fields[prefix] = element.GetString()!;
                        _logger.LogDebug("Extracted string field: {Key} = {Value}", prefix, element.GetString());
                    }
                    break;

                case JsonValueKind.Number:
                case JsonValueKind.True:
                case JsonValueKind.False:
                    fields[prefix] = element.ToString();
                    _logger.LogDebug("Extracted primitive field: {Key} = {Value}", prefix, element.ToString());
                    break;
            }
        }


        private void ParseStringResponse(string documentString, Dictionary<string, string> fields)
        {
            var lines = documentString.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            string? currentField = null;

            foreach (var line in lines.Select(l => l.Trim()))
            {
                if (line.StartsWith(":") && line.EndsWith(":") && !line.Contains(" "))
                {

                    currentField = line.Trim(':');
                }
                else if (line.StartsWith(":value:") && currentField != null)
                {
                    var value = line.Substring(7).Trim(); 
                    if (!string.IsNullOrWhiteSpace(value) && value != "null" && value != "N/A")
                    {
                        fields[currentField] = value;
                        _logger.LogDebug("Parsed field from text: {Key} = {Value}", currentField, value);
                    }

                    currentField = null; 
                }
            }
        }

    }
}