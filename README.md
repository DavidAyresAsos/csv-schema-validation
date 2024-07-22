# csv-schema-utility

## Introduction

The CSV Schema Utility tool is a bespoke class library that will take a CSV file and a JSON schema file, than validate that the CSV content is compliant to that schema. Under the hood it's taking the CSV, translating it into a JSON object and then applying the JSON schema validation. By converting the CSV file it means we can utilise native JSON schema validation libraries and not have to write our own.

## Why JSON Schema?

CSV is a hugely neglected data type, although it's often seen as an "older" format and often overlooked when choosing a data integration format, it sill offers immense value when transferring large volumes of data between systems. There's a couple of Schema formats out there at different levels of maturity but their biggest obstacles that their are either dormant or don't have any active development to support the .NET platform.

Examples:
- CSV on the Web: (https://csvw.org/)
- CSV Schema: (https://digital-preservation.github.io/csv-schema/)

After an evaluation period, these were both deemed not mature enough to be adopted in a Enterprise landscape. JSON Schema is already an active and adopted schema notation approach for JSON data formats, and applies directly to APIs predominently, where it's the defacto standard at ASOS as defined by the API Working Group. By taking an active, mature and widely adopted notation it means we can adopt all of the richness and long term support that it offers while pivoting to a CSV format.

## JSON Schema Overview

This has been as pure and direct an integration as possible. At this point in time it only supports 1 Schema per CSV file. It could be argued that 1 CSV file with multiple schemas contained within it goes against the fundamental design of what a CSV file is but at ASOS we have production files that have multiple schemas, so these have had to be descoped at this point in time.

As JSON Schema allows an ifninte number of nested items, there have had to be some compromises to the level of validation that is supported. A CSV, by design, is only ever a single level data object. When defining your JSON schema, it'll only ever by at the first node level.

Then when it comes to supported data types, we've purposefully forced the code to throw an exception if an array type is defined, as that's not compliant with a CSV data format.

So we only support:
- string
- number (Converts to a double)
- integer (Converts to an int)
- object
- boolean
- null

For strings currently, the keyword specific [Built-In formats](https://json-schema.org/understanding-json-schema/reference/string) of a string are all treated the same and there's no custom validation applied to them. This can be added at a later date.

### Example

```
{
  "$id": "https://example.com/person.schema.json",
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "title": "Person",
  "type": "object",
  "csvDelimiter": ",",
  "csvHasHeader": "true",
  "properties": {
    "firstName": {
      "type": "string",
      "description": "The person's first name."
    },
    "lastName": {
      "type": "string",
      "description": "The person's last name."
    },
    "age": {
      "description": "Age in years which must be equal to or greater than zero.",
      "type": "integer",
      "minimum": 0
    }
  }
}
```

- firstName and lastName; string, no minimum or maximum length - so if there's no first name, leave it empty and comma seperated and it'll pass.
- age; integer so would throw an error if a string or double value was entered. Will also output an error if the age is negative.

### Custom JSON Schema notation

When working with a CSV, there's a couple of key facts you need to know about the document:

- Does it have a hgeader?
- What's the delimiter to split the file on?

To support this, 2 new JSON Schema fields have been added that ALL specification must provide. If they aren't present, or the values not as expected, the tool will throw a Validation Error.

- "csvDelimiter"; has to be a char character.
- "csvHasHeader"; either true or false.

### Library

For JSON schema validation we are using the Json.Schema library provided by [JSON Everything](https://github.com/gregsdennis/json-everything)

## CSV Parsing

The CSV parsing is relatively straight forward, although the tool does have to make some assumptions about the data, that it takes from the Schema.

By using the library [CSVHelper](https://joshclose.github.io/CsvHelper/) we can pass in the delimiter and header flag, to return a dynamic array of fields and rows. If that returns now rows, we throw an error as we can't validate empty files. What this library allows us to do is support speech marks around string content, so we can protect text that otherwise might be delimtited on.

For example here is our basic test data, where we have a header row and a comma delimiter:

```
firstName,lastName,age
John,Fish,5
David,Ayres,22
```

But we could have a comma in the name, which would make the input file the below, which passes:

```
firstName,lastName,age
John,"Fish, Snr",5
David,Ayres,22
```

Then not strictly compliant to the CSV specification, you could end up with carriage returns of other special characters in a string field, so the below also passes and John's details are parsed as 1 row of data:

```
firstName,lastName,age
John,"Fish,
Snr",5
David,Ayres,22
```
Then, that object is checked to make sure that the number of columns per rows matches the number of columns in the JSON schema file. If it does not, then the row will be rejected as being invalid.

## Conversion to JSON

So the application will load the schema into memory as a Schema Object, load the CSV file into memory as a dynamic array and it's at this point we then go row by row, converting to JSON and validating.

The conversion routine is very simple;

- it will loop through each column in the row
- take the "property" name from the corresponding column of data from the schema
- use this as the field name
- it'll then take the Schema Value Type from corresponding column
- try to parse the column's data into that format, if it passes it's added as the converted type
- otherwise it'll add it as an object and let JSON schema validation handle it

These are added to a dictionary, a complex represtnation of the CSV row with column header names from the schema, which is then passes to the Serialize function of System.Text.Json.JsonSerializer to convert to JSON.

We then pass that JSON object into System.Text.Json.JsonDocument.ParseAsync to get our results.

Note: Your JSON Schema columns must be in exactly the same order as the CSV, otherwise it won't be able to map them accurately and your data will fail validation. As a CSV can be headerless, the only way to associate a JSON Schema property to the correct column is via positioning.

## Feedback

We use a custom object to collect the validation results and return this, as a list. It's a simple class with only 2 fields;

```
public bool IsValid { get; set; }
public string Message { get; set; }
```

If the field/column passes validation, we set that to "true", otherwise it's "false".

Then the message is a combination of the row id, field name and any validation messages that are present.

Examples:
- "Line 1 is valid." - that means the CSV line has passed validation. Note: if there's a header, the line number will start at 2.
- "Error on line 3 and column "Sku_Id"; Value should be at least 1 characters" - this looks like a mandatory field, Sku_Id is empty.
- "Error on line 20 and column "Quantity"; -1 should be at least 0"

If you want to validate the file passed? You just check to make sure ALL the IsValid values returned in the list are true.

### Multiple Schemas per File
If there was an urgent requirement to implement this level of validation, an Engineer could work around this by putting some custom code in place to split the original file into lists of data per schema, then send those lists with their appropriate schema file into the validation tool. It means it won't work "out the box" but with some custom arrangement of the data, this tool can still offer some value.
