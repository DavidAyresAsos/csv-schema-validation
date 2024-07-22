using CsvHelper;
using CsvHelper.Configuration;
using Json.Schema;
using System.Diagnostics;
using System.Formats.Asn1;
using System.Globalization;
using System.Text.Json;

namespace CSVSchemaImplementation
{
    public class SchemaReader
    {
        public static async Task<ValidationResponseModel> ValidateSchema(string schema, Stream csvContent)
        {
            return await ProcessCSV(csvContent, await LoadSchemaByText(schema));
        }

        public static async Task<ValidationResponseModel> ValidateSchema(Stream schema, Stream csvContent)
        {
            return await ProcessCSV(csvContent, await LoadSchemaByStream(schema));
        }

        private static Task<JsonSchema> LoadSchemaByText(string schema)
        {
            return Task.FromResult(JsonSchema.FromText(schema));
        }

        private static async Task<JsonSchema> LoadSchemaByStream(Stream schema)
        {
            return await JsonSchema.FromStream(schema);
        }

        private static async Task<ValidationResponseModel> ProcessCSV(Stream csvContent, JsonSchema schema)
        {
            var validationResponse = new ValidationResponseModel();
            var validationResults = new List<ValidationResultModel>();
            var (csvDelimiter, csvHasHeader, schemaValidationResults) = ParseSchema(schema);

            if (schemaValidationResults.Any())
            {
                validationResults.AddRange(schemaValidationResults);
            }
            else
            {
                using (var reader = new StreamReader(csvContent))
                using (var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { Delimiter = csvDelimiter.ToString(), HasHeaderRecord = csvHasHeader }))
                {
                    var records = csv.GetRecords<dynamic>().ToList();

                    if (records is null || records.Count == 0)
                    {
                        validationResults.Add(new ValidationResultModel(-1, false, "Error", $"CSV content is empty, please check your input data"));
                    }
                    else
                    {
                        var headerInfo = ExtractHeaderInfo(schema);
                        var lineCount = 1;

                        foreach (var record in records)
                        {
                            var (headerNames, headerValues) = headerInfo;
                            var values = ((System.Dynamic.ExpandoObject)record).Select(x => x.Value is null ? null : x.Value.ToString()).ToArray();

                            if (values.Length != headerNames.Count)
                            {
                                validationResults.Add(new ValidationResultModel(lineCount, false, "Error", $"Expected columns - {headerNames.Count} but provided columns - {values.Length}"));
                                continue;
                            }

                            var row = CreateRow(values, headerNames, headerValues);
                            var jsonResult = await ValidateAndSerializeRow(schema, row);

                            if (!jsonResult.IsValid)
                            {
                                validationResults.AddRange(EvaluateValidationErrors(jsonResult, lineCount));
                            }
                            else
                            {
                                validationResults.Add(new ValidationResultModel(lineCount, true, "-", $"Valid"));
                            }

                            lineCount++;
                        }
                    }
                }
            }

            validationResponse.IsValid = !validationResults.Any(x => !x.IsValid);
            validationResponse.Results = validationResults;

            return validationResponse;
        }

        private static (char, bool, List<ValidationResultModel>) ParseSchema(JsonSchema schema)
        {
            var validationResults = new List<ValidationResultModel>();
            var keywords = schema.Keywords;

            char csvDelimiter = ',';
            var csvHasHeader = true;
            var hasCsvDelimiter = false;
            var hasCsvHasHeader = false;

            if (keywords is null)
            {
                validationResults.Add(new ValidationResultModel(-1, false, "Error", $"Schema keywords are null, there could be an issue with your file, please check"));
            }
            else
            {
                foreach (var keyword in keywords)
                {
                    if (keyword is UnrecognizedKeyword _keyword)
                    {
                        if (_keyword.Name == "csvDelimiter")
                        {
                            hasCsvDelimiter = true;

                            if (_keyword.Value == null || string.IsNullOrEmpty((string?)_keyword.Value))
                            {
                                validationResults.Add(new ValidationResultModel(-1, false, "Error", $"csvDelimiter must have a value"));
                            }
                            else if (_keyword.Value.ToString().Length > 1)
                            {
                                validationResults.Add(new ValidationResultModel(-1, false, "Error", $"csvDelimiter can only be a single character"));
                            }
                            else
                            {
                                csvDelimiter = _keyword.Value.ToString().First();
                            }
                        }
                        else if (_keyword.Name == "csvHasHeader")
                        {
                            hasCsvHasHeader = true;

                            if (_keyword.Value == null || string.IsNullOrEmpty((string?)_keyword.Value))
                            {
                                validationResults.Add(new ValidationResultModel(-1, false, "Error", $"csvHasHeader must have a value"));
                            }
                            else if (!bool.TryParse((string)_keyword.Value, out bool _output))
                            {
                                validationResults.Add(new ValidationResultModel(-1, false, "Error", $"csvHasHeader must be true or false"));
                            }
                            else
                            {
                                csvHasHeader = _output;
                            }
                        }
                    }
                }

                if (!hasCsvDelimiter)
                {
                    validationResults.Add(new ValidationResultModel(-1, false, "Error", $"Schema does not include: csvDelimiter which is mandatory"));
                }

                if (!hasCsvHasHeader)
                {
                    validationResults.Add(new ValidationResultModel(-1, false, "Error", $"Schema does not include: csvHasHeader which is mandatory"));
                }
            }

            return (csvDelimiter, csvHasHeader, validationResults);
        }

        private static (List<string>, List<SchemaValueType>) ExtractHeaderInfo(JsonSchema schema)
        {
            var properties = (PropertiesKeyword)schema["properties"];
            var headerNames = new List<string>();
            var headerValues = new List<SchemaValueType>();

            foreach (var property in properties.Properties)
            {
                headerNames.Add(property.Key);

                foreach (var keyword in property.Value.Keywords)
                {
                    if (keyword is TypeKeyword typeKeyword)
                    {
                        headerValues.Add(typeKeyword.Type);
                        break;
                    }
                }
            }

            return (headerNames, headerValues);
        }

        private static Dictionary<string, object> CreateRow(string[] values, List<string> headerNames, List<SchemaValueType> headerValues)
        {
            var row = new Dictionary<string, object>();

            for (int j = 0; j < headerNames.Count && j < values.Length; j++)
            {
                var typeFormat = headerValues[j];
                object valueToAdd = string.Empty;

                switch (typeFormat)
                {
                    case SchemaValueType.Number:
                    case SchemaValueType.Number | SchemaValueType.Null:
                        if (values[j] is null)
                        {
                            valueToAdd = values[j];
                        }
                        else
                        {
                            if (double.TryParse(values[j], out double doubleResult))
                                valueToAdd = doubleResult;
                            else
                                valueToAdd = values[j];
                        }
                        break;
                    case SchemaValueType.Integer:
                    case SchemaValueType.Integer | SchemaValueType.Null:
                        if (values[j] is null)
                        {
                            valueToAdd = values[j];
                        }
                        else
                        {
                            if (int.TryParse(values[j], out int intResult))
                                valueToAdd = intResult;
                            else
                                valueToAdd = values[j];
                        }
                        break;
                    case SchemaValueType.Boolean:
                    case SchemaValueType.Boolean | SchemaValueType.Null:
                        if (values[j] is null)
                        {
                            valueToAdd = values[j];
                        }
                        else
                        {
                            if (bool.TryParse(values[j], out bool boolResult))
                                valueToAdd = boolResult;
                            else
                                valueToAdd = values[j];
                        }
                        break;
                    default:
                        valueToAdd = values[j];
                        break;
                }

                row.Add(headerNames[j], valueToAdd);
            }

            return row;
        }

        private static async Task<EvaluationResults> ValidateAndSerializeRow(JsonSchema schema, Dictionary<string, object> row)
        {
            var jsonSerialized = JsonSerializer.Serialize(row);
            var jsonDocLine = await JsonDocument.ParseAsync(GenerateStreamFromString(jsonSerialized));

            return schema.Evaluate(jsonDocLine, new EvaluationOptions
            {
                RequireFormatValidation = true,
                OutputFormat = OutputFormat.List
            });
        }

        private static List<ValidationResultModel> EvaluateValidationErrors(EvaluationResults jsonResult, int lineCount)
        {
            var validationResults = new List<ValidationResultModel>();

            foreach (var line in jsonResult.Details)
            {
                if (!line.IsValid && line.Errors != null && line.Errors.Any())
                {
                    foreach (var error in line.Errors)
                    {
                        var instanceLocation = line.InstanceLocation;

                        validationResults.Add(new ValidationResultModel(lineCount, false, instanceLocation.Segments.Any() ? line.InstanceLocation.ToString().Replace("/", "") : "-", error.Value));
                    }
                }
            }

            return validationResults;
        }

        public static MemoryStream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}