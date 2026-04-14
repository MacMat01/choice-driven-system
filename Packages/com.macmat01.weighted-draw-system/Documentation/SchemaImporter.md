# SchemaImporter

## What This Module Does

`SchemaImporter` turns CSV or JSON text into validated `DataRecord` objects using a `DataSchemaSO` definition.

It handles:

- required-field validation
- type conversion (`String`, `Int`, `Float`, `Bool`, `ConditionList`)
- case-insensitive field matching
- parsing condition expressions into `ParsedCondition` lists

Use it when designers own data tables and you want predictable runtime data with clear validation behavior.

## Core Workflow

### 1) Prepare data

Example CSV:

```csv
ItemID,ItemName,Weight,Conditions
sword_common,Iron Sword,1.0,
sword_rare,Dragon Blade,0.2,playerLevel >= 10
```

Example JSON:

```json
[
  {
    "ItemID": "sword_common",
    "ItemName": "Iron Sword",
    "Weight": 1.0,
    "Conditions": ""
  },
  {
    "ItemID": "sword_rare",
    "ItemName": "Dragon Blade",
    "Weight": 0.2,
    "Conditions": "playerLevel >= 10"
  }
]
```

### 2) Define a schema (`DataSchemaSO`)

Create a schema asset and configure columns:

- `ItemID` -> `String` (Required)
- `ItemName` -> `String` (Required)
- `Weight` -> `Float` (Required)
- `Conditions` -> `ConditionList` (Optional)

Assign your CSV/JSON file to `Source Data File`.

### 3) Import data

```csharp
DataSchemaSO schema = Resources.Load<DataSchemaSO>("LootDropSchema");
List<DataRecord> records = DynamicDataImporter.ImportFromSchema(schema);
```

`records` now contains validated rows/objects with converted values.

## Import API

### `ImportFromSchema(DataSchemaSO schema)`

Primary entry point. Reads from `schema.SourceDataFile` and auto-detects CSV vs JSON.

You can extend format support by registering custom parsers through `DynamicDataImporter.RegisterParser(...)`.

The built-in routes are provided by the package's internal extension adapters and dispatch to `CsvDataParser` and `JsonDataParser`.

### `ImportFromTextAsset(TextAsset textAsset, DataSchemaSO schema)`

Use when the source text asset is provided directly.

### `ImportFromFilePath(string filePath, DataSchemaSO schema)`

Use for explicit path-based loading.

## Reading Values

`DataRecord` is dictionary-like and case-insensitive by key.

```csharp
DataRecord record = records[0];

string id = record.GetField("ItemID")?.ToString();
float weight = (float?)record.GetField("Weight") ?? 1f;
List<ParsedCondition> conditions =
    record.GetField("Conditions") as List<ParsedCondition>;

if (record.TryGetField("ItemID", out object rawId))
{
    Debug.Log(rawId);
}
```

## Condition Syntax

`ConditionList` strings are parsed by `ConditionParserUtility`.

- operators: `==`, `!=`, `>`, `<`, `>=`, `<=`
- AND connectors: `&&`, `&`, `and`, `;`
- OR connectors: `||`, `|`, `or`
- shorthand: `flag` means `flag == 1`, `!flag` means `flag != 1`

The connector and operator vocabulary is shared with runtime evaluation through `ConditionSemantics`.

Examples:

```text
playerLevel >= 10 && hasWeapon == 1
isNight or isDungeon
!isDead
```

Malformed segments are skipped where possible, and warnings are logged.

## Conversion And Validation Behavior

- Required columns missing from source data are reported; invalid rows/items are skipped.
- Required values that are empty, whitespace, or null make that row/item invalid.
- Invalid numeric/bool conversions log warnings and use defaults (`0`, `0f`, `false`).
- Empty condition strings convert to an empty condition list.
- Explicit JSON `null` values remain `null` in the resulting `DataRecord`.

## JSON Nested Data

Nested JSON objects are flattened so a flat schema can still bind values.

Example nested key aliases include:

- `Parent_Child_Value`
- parent + leaf shortcuts where applicable

This allows nested authoring while keeping schema fields simple.

## Integration With ProbabilityEngine

You can feed imported records directly into `RandomiserSystem` for weighted selection:

```csharp
List<DataRecord> records = DynamicDataImporter.ImportFromSchema(schema);
var randomiser = new RandomiserSystem(records, schema);

var context = new Dictionary<string, object>
{
    { "playerLevel", 10 },
    { "hasWeapon", 1 }
};

DataRecord selected = randomiser.EvaluateRandom(context);
```

## Best Practices

- Keep column names stable and explicit.
- Mark fields as required only when truly required.
- Treat importer warnings as data quality issues to fix early.
- Keep condition expressions simple and testable.
- Cover your data path with Edit Mode tests.

## Related Files

- Runtime code: `Runtime/SchemaImporter`
- Tests: `Tests/EditMode/SchemaImporter`
- Engine docs: `Documentation/ProbabilityEngine.md`
