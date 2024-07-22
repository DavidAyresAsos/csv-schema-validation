using CsvHelper;
using CsvHelper.Configuration;
using CSVSchemaImplementation;
using Json.Schema;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

namespace CSVSchemaTest
{
    internal class Program
    {
        public static string csvDelimiterName = "csvDelimiter";
        public static string csvHasHeaderName = "csvHasHeader";

        static async Task Main(string[] args)
        {
            await ProcessFileAndSchema(
                "names.schema.json",
                "names.csv");
        }

        static async Task ProcessFileAndSchema(string schemaPath, string csvPath)
        {
            FileStream filestream = new($"result_{DateTime.Now.Ticks}.txt", FileMode.Create);
            var streamwriter = new StreamWriter(filestream)
            {
                AutoFlush = true
            };

            Console.SetOut(streamwriter);
            Console.SetError(streamwriter);

            Console.WriteLine("########## Processing File ##########");
            Console.WriteLine();

            OutputFileName(schemaPath, "Schema");
            OutputFileName(csvPath, "CSV");
            Console.WriteLine();

            var timer = new Stopwatch();
            timer.Start();

            var response = await SchemaReader.ValidateSchema(
                File.ReadAllText(schemaPath),
                File.OpenRead(csvPath));

            var valid = response.IsValid;

            Console.WriteLine($"File Valid: {(valid ? "Passed" : "Failed")}");
            Console.WriteLine();

            foreach (var line in response.Results)
            {
                OutputErrorMessage(line);
            }

            Console.WriteLine();
            Console.WriteLine($"Time Elapsed: {timer.Elapsed.Hours}:{timer.Elapsed.Minutes}:{timer.Elapsed.Seconds}");
            Console.WriteLine();
        }

        static void OutputFileName(string path, string type)
        {
            var fileInfo = new FileInfo(path);

            Console.WriteLine($"Processing {type}: {fileInfo.Name}");
        }

        static void OutputErrorMessage(ValidationResultModel line)
        {
            if (line.IsValid)
            {
                Console.WriteLine($"Line {line.LineNumber} is valid");
            }
            else
            {
                if (line.LineNumber == -1)
                {
                    Console.WriteLine($"Pre-processing error: {line.Message}");
                }
                else
                {
                    Console.WriteLine($"Error on line: {line.LineNumber} with column: {line.Column}; {line.Message}");
                }
            }
        }
    }
}