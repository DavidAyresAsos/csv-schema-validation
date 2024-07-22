using CsvHelper;
using CSVSchemaImplementation;
using NUnit.Framework;
using System.Text.Json;

namespace CSVSchemaImplementationTests
{
    [TestFixture]
    public class SchemaReaderTests
    {
        [Test]
        public async Task ValidateSchemaValidCSVReturnsValidResult()
        {
            // Arrange
            var schema = "{\"$id\":\"https://example.com/person.schema.json\",\"$schema\":\"https://json-schema.org/draft/2020-12/schema\",\"title\":\"Person\",\"type\":\"object\",\"csvDelimiter\":\",\",\"csvHasHeader\":\"true\",\"properties\":{\"firstName\":{\"type\":\"string\",\"description\":\"The person's first name.\"},\"lastName\":{\"type\":\"string\",\"description\":\"The person's last name.\"},\"age\":{\"description\":\"Age in years which must be equal to or greater than zero.\",\"type\":\"integer\",\"minimum\":0}}}";
            var csvContent = "firstName,lastName,age\nJohn,Fish,5\nDavid,Ayres,22";

            // Act
            var validationResponse = await SchemaReader.ValidateSchema(schema, SchemaReader.GenerateStreamFromString(csvContent));

            // Assert
            Assert.That(validationResponse.IsValid, Is.True);

            var validationResult = validationResponse.Results;

            Assert.That(validationResult.Count, Is.EqualTo(2));

            Assert.That(validationResult[0].IsValid, Is.True);
            Assert.That(validationResult[0].Message, Is.EqualTo("Valid"));

            Assert.That(validationResult[1].IsValid, Is.True);
            Assert.That(validationResult[1].Message, Is.EqualTo("Valid"));
        }

        [Test]
        public async Task ValidateSchemaInvalidCSVReturnsInvalidResult()
        {
            // Arrange
            var schema = "{\"$id\":\"https://example.com/person.schema.json\",\"$schema\":\"https://json-schema.org/draft/2020-12/schema\",\"title\":\"Person\",\"type\":\"object\",\"csvDelimiter\":\",\",\"csvHasHeader\":\"true\",\"properties\":{\"firstName\":{\"type\":\"string\",\"description\":\"The person's first name.\"},\"lastName\":{\"type\":\"string\",\"description\":\"The person's last name.\"},\"age\":{\"description\":\"Age in years which must be equal to or greater than zero.\",\"type\":\"integer\",\"minimum\":0}}}";
            var csvContent = "firstName,lastName,age\nJohn,Fish,ERROR\nDavid,Ayres,22";

            // Act
            var validationResponse = await SchemaReader.ValidateSchema(schema, SchemaReader.GenerateStreamFromString(csvContent));

            // Assert
            Assert.That(validationResponse.IsValid, Is.False);

            var validationResult = validationResponse.Results;

            Assert.That(validationResult.Count, Is.EqualTo(2));

            Assert.That(validationResult[0].IsValid, Is.False);
            Assert.That(validationResult[0].Message, Is.EqualTo("Value is \"string\" but should be \"integer\""));

            Assert.That(validationResult[1].IsValid, Is.True);
            Assert.That(validationResult[1].Message, Is.EqualTo("Valid"));
        }

        [Test]
        public void ValidateSchemaNullSchemaThrowsException()
        {
            // Arrange
            string? schema = null;
            var csvContent = "Name,Age\nJohn,30\nAlice,25";

            //Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
            {
                await SchemaReader.ValidateSchema(schema, SchemaReader.GenerateStreamFromString(csvContent));
            });
        }

        [Test]
        public async Task ValidateSchemaNullCSVContentThrowsException()
        {
            // Arrange
            var schema = "{\"$id\":\"https://example.com/person.schema.json\",\"$schema\":\"https://json-schema.org/draft/2020-12/schema\",\"title\":\"Person\",\"type\":\"object\",\"csvDelimiter\":\",\",\"csvHasHeader\":\"true\",\"properties\":{\"firstName\":{\"type\":\"string\",\"description\":\"The person's first name.\"},\"lastName\":{\"type\":\"string\",\"description\":\"The person's last name.\"},\"age\":{\"description\":\"Age in years which must be equal to or greater than zero.\",\"type\":\"integer\",\"minimum\":0}}}";

            // Act & Assert
            var validationResponse = await SchemaReader.ValidateSchema(schema, SchemaReader.GenerateStreamFromString(null));

            // Assert
            Assert.That(validationResponse.IsValid, Is.False);

            var validationResult = validationResponse.Results;

            Assert.That(validationResult.Count, Is.EqualTo(1));

            Assert.That(validationResult[0].IsValid, Is.False);
            Assert.That(validationResult[0].Message, Is.EqualTo("CSV content is empty, please check your input data"));
        }

        [Test]
        public async Task ValidateSchemaEmptyCSVContentThrowsException()
        {
            // Arrange
            var schema = "{\"$id\":\"https://example.com/person.schema.json\",\"$schema\":\"https://json-schema.org/draft/2020-12/schema\",\"title\":\"Person\",\"type\":\"object\",\"csvDelimiter\":\",\",\"csvHasHeader\":\"true\",\"properties\":{\"firstName\":{\"type\":\"string\",\"description\":\"The person's first name.\"},\"lastName\":{\"type\":\"string\",\"description\":\"The person's last name.\"},\"age\":{\"description\":\"Age in years which must be equal to or greater than zero.\",\"type\":\"integer\",\"minimum\":0}}}";

            // Act & Assert
            var validationResponse = await SchemaReader.ValidateSchema(schema, SchemaReader.GenerateStreamFromString(string.Empty));

            // Assert
            Assert.That(validationResponse.IsValid, Is.False);

            var validationResult = validationResponse.Results;

            Assert.That(validationResult.Count, Is.EqualTo(1));

            Assert.That(validationResult[0].IsValid, Is.False);
            Assert.That(validationResult[0].Message, Is.EqualTo("CSV content is empty, please check your input data"));
        }

        [Test]
        public void ValidateSchemaInvalidSchemaThrowsException()
        {
            // Arrange
            var schema = "InvalidSchema";
            var csvContent = "Name,Age\nJohn,30\nAlice,25";

            // Act & Assert
            Assert.ThrowsAsync<JsonException>(async () =>
            {
                await SchemaReader.ValidateSchema(schema, SchemaReader.GenerateStreamFromString(csvContent));
            });
        }

        [Test]
        public async Task ValidateSchemaInvalidCSVContentThrowsExceptionAsync()
        {
            // Arrange
            var schema = "{ \"properties\": { \"Name\": { \"type\": \"string\" }, \"Age\": { \"type\": \"integer\" } }, \"csvDelimiter\": \",\", \"csvHasHeader\": \"true\" }";

            // Act
            var validationResponse = await SchemaReader.ValidateSchema(schema, SchemaReader.GenerateStreamFromString(string.Empty));

            // Assert
            Assert.That(validationResponse.IsValid, Is.False);

            var validationResult = validationResponse.Results;

            Assert.That(validationResult.Count, Is.EqualTo(1));

            Assert.That(validationResult[0].IsValid, Is.False);
            Assert.That(validationResult[0].Message, Is.EqualTo("CSV content is empty, please check your input data"));
        }

        [Test]
        public async Task ValidateSchemaCSVDelimiterEmpty()
        {
            // Arrange
            var schema = "{ \"properties\": { \"Name\": { \"type\": \"string\" }, \"Age\": { \"type\": \"integer\" } }, \"csvDelimiter\": \"\", \"csvHasHeader\": \"true\" }";
            var csvContent = "Name,Age\nJohn,30\nAlice,25";

            // Act
            var validationResponse = await SchemaReader.ValidateSchema(schema, SchemaReader.GenerateStreamFromString(csvContent));

            // Assert
            Assert.That(validationResponse.IsValid, Is.False);

            var validationResult = validationResponse.Results;

            Assert.That(validationResult.Count, Is.EqualTo(1));

            Assert.That(validationResult[0].IsValid, Is.False);
            Assert.That(validationResult[0].Message, Is.EqualTo("csvDelimiter must have a value"));
        }

        [Test]
        public async Task ValidateSchemaCSVDelimiter_StringNotChar()
        {
            // Arrange
            var schema = "{ \"properties\": { \"Name\": { \"type\": \"string\" }, \"Age\": { \"type\": \"integer\" } }, \"csvDelimiter\": \"aaa\", \"csvHasHeader\": \"true\" }";
            var csvContent = "Name,Age\nJohn,30\nAlice,25";

            // Act
            var validationResponse = await SchemaReader.ValidateSchema(schema, SchemaReader.GenerateStreamFromString(csvContent));

            // Assert
            Assert.That(validationResponse.IsValid, Is.False);

            var validationResult = validationResponse.Results;

            Assert.That(validationResult.Count, Is.EqualTo(1));

            Assert.That(validationResult[0].IsValid, Is.False);
            Assert.That(validationResult[0].Message, Is.EqualTo("csvDelimiter can only be a single character"));
        }

        [Test]
        public async Task ValidateSchemaCSVDelimiterMissing()
        {
            // Arrange
            var schema = "{ \"properties\": { \"Name\": { \"type\": \"string\" }, \"Age\": { \"type\": \"integer\" } }, \"csvHasHeader\": \"true\" }";
            var csvContent = "Name,Age\nJohn,30\nAlice,25";

            // Act
            var validationResponse = await SchemaReader.ValidateSchema(schema, SchemaReader.GenerateStreamFromString(csvContent));

            // Assert
            Assert.That(validationResponse.IsValid, Is.False);

            var validationResult = validationResponse.Results;

            Assert.That(validationResult.Count, Is.EqualTo(1));

            Assert.That(validationResult[0].IsValid, Is.False);
            Assert.That(validationResult[0].Message, Is.EqualTo("Schema does not include: csvDelimiter which is mandatory"));
        }

        [Test]
        public async Task ValidateSchemaCSVHeaderEmpty()
        {
            // Arrange
            var schema = "{ \"properties\": { \"Name\": { \"type\": \"string\" }, \"Age\": { \"type\": \"integer\" } }, \"csvDelimiter\": \",\", \"csvHasHeader\": null }";
            var csvContent = "Name,Age\nJohn,30\nAlice,25";

            // Act
            var validationResponse = await SchemaReader.ValidateSchema(schema, SchemaReader.GenerateStreamFromString(csvContent));

            // Assert
            Assert.That(validationResponse.IsValid, Is.False);

            var validationResult = validationResponse.Results;

            Assert.That(validationResult.Count, Is.EqualTo(1));

            Assert.That(validationResult[0].IsValid, Is.False);
            Assert.That(validationResult[0].Message, Is.EqualTo("csvHasHeader must have a value"));
        }

        [Test]
        public async Task ValidateSchemaCSVHeaderString()
        {
            // Arrange
            var schema = "{ \"properties\": { \"Name\": { \"type\": \"string\" }, \"Age\": { \"type\": \"integer\" } }, \"csvDelimiter\": \",\", \"csvHasHeader\": \"HelloWorld\" }";
            var csvContent = "Name,Age\nJohn,30\nAlice,25";

            // Act
            var validationResponse = await SchemaReader.ValidateSchema(schema, SchemaReader.GenerateStreamFromString(csvContent));

            // Assert
            Assert.That(validationResponse.IsValid, Is.False);

            var validationResult = validationResponse.Results;

            Assert.That(validationResult.Count, Is.EqualTo(1));

            Assert.That(validationResult[0].IsValid, Is.False);
            Assert.That(validationResult[0].Message, Is.EqualTo("csvHasHeader must be true or false"));
        }

        [Test]
        public async Task ValidateSchemaCSVHeaderMissing()
        {
            // Arrange
            var schema = "{ \"properties\": { \"Name\": { \"type\": \"string\" }, \"Age\": { \"type\": \"integer\" } }, \"csvDelimiter\": \",\" }";
            var csvContent = "Name,Age\nJohn,30\nAlice,25";

            // Act
            var validationResponse = await SchemaReader.ValidateSchema(schema, SchemaReader.GenerateStreamFromString(csvContent));

            // Assert
            Assert.That(validationResponse.IsValid, Is.False);

            var validationResult = validationResponse.Results;

            Assert.That(validationResult.Count, Is.EqualTo(1));

            Assert.That(validationResult[0].IsValid, Is.False);
            Assert.That(validationResult[0].Message, Is.EqualTo("Schema does not include: csvHasHeader which is mandatory"));
        }
    }
}
