# SchemaImporter Documentation

## Overview

The **SchemaImporter** converts raw data files (CSV or JSON) into strongly-typed `DataRecord` objects using a schema definition. Think of it as a "data validator and type converter" that lets you define what columns/fields you expect, what type each should be, and which are required.

**Key Features:**
- ✅ CSV and JSON support
- ✅ Type conversion (String, Int, Float, Bool, ConditionList)
- ✅ Required field validation
- ✅ Case-insensitive column/property matching
- ✅ Flexible condition syntax parsing
- ✅ JSON nested object flattening
- ✅ Robust error handling (skips invalid rows/objects, logs warnings)

## Why You Need It

**Scenario:** You have a CSV file with loot drops, dialogue lines, or procedural events. You want to:
- ✅ Ensure required fields exist
- ✅ Convert "10" (text) to 10 (number)
- ✅ Parse conditions like `"playerLevel >= 5 && hasWeapon == true"`
- ✅ Get a strongly-typed collection you can work with in code

SchemaImporter does all of that automatically.

## Quick Start

### Step 1: Create Your Data File

Create a CSV or JSON file with your data. Example CSV:

```csv
ItemID,ItemName,Rarity,Weight,Conditions
sword_common,Iron Sword,Common,1.0,
sword_rare,Dragon Blade,Rare,0.3,playerLevel >= 10
helmet_epic,Crown of Kings,Epic,0.1,playerLevel >= 20 && hasGold == 1
```

Example JSON:

```json
[
  {
    "ItemID": "sword_common",
    "ItemName": "Iron Sword",
    "Rarity": "Common",
    "Weight": 1.0,
    "Conditions": ""
  },
  {
    "ItemID": "sword_rare",
    "ItemName": "Dragon Blade",
    "Rarity": "Rare",
    "Weight": 0.3,
    "Conditions": "playerLevel >= 10"
  }
]
```

### Step 2: Create a DataSchemaSO Asset

1. Right-click in your project → `Create > SchemaImporter > Data Schema`
2. Name it meaningfully (e.g., `LootDropSchema`)
3. In the Inspector, add entries under **Columns** for each column/field in your data:

| Field | Value |
|-------|-------|
| Column Name | `ItemID` |
| Data Type | `String` |
| Is Required | ✓ (checked) |

4. Repeat for each column:
   - `ItemName` → String, Required
   - `Rarity` → String, Not Required
   - `Weight` → Float, Required
   - `Conditions` → ConditionList, Not Required

5. Drag your data file into the **Source Data File** field

### Step 3: Verify Your Data

You're done! The schema is now configured. Your data file will be automatically validated and parsed when imported.

**What the schema does:**
- ✅ Ensures required columns exist
- ✅ Ensures required fields are not empty
- ✅ Converts values to the correct type
- ✅ Skips invalid rows and logs warnings
- ✅ Parses condition expressions into a structured format

## API Reference

### Importing Data

The `DynamicDataImporter` class provides three methods to import data:

#### `ImportFromSchema(DataSchemaSO schema)`

The primary method. Automatically detects file format from the schema's source file.

```csharp
DataSchemaSO schema = Resources.Load<DataSchemaSO>("LootDropSchema");
List<DataRecord> records = DynamicDataImporter.ImportFromSchema(schema);
```

**Throws:**
- `ArgumentNullException` if `schema` is `null`
- `InvalidOperationException` if `schema.SourceDataFile` is missing

**Returns:**
- List of validated `DataRecord` objects
- Invalid rows are skipped and logged as warnings
- Empty list if file format is unsupported

#### `ImportFromTextAsset(TextAsset textAsset, DataSchemaSO schema)`

Import from an explicit TextAsset.

```csharp
TextAsset data = Resources.Load<TextAsset>("loot_drops");
List<DataRecord> records = DynamicDataImporter.ImportFromTextAsset(data, schema);
```

**Returns:**
- Empty list if `textAsset` is `null` or `schema` is `null`

#### `ImportFromFilePath(string filePath, DataSchemaSO schema)`

Import directly from a file path (useful for runtime file loading).

```csharp
List<DataRecord> records = DynamicDataImporter.ImportFromFilePath("Assets/Data/loot.csv", schema);
```

**Returns:**
- Empty list if `filePath` is blank, doesn't exist, or `schema` is `null`

### DataRecord

Each imported row/object becomes a `DataRecord` - a dictionary-like structure:

```csharp
DataRecord record = records[0];

// Get by column name (case-insensitive)
string itemName = record.GetField("ItemName")?.ToString();
int itemLevel = (int?)record.GetField("ItemLevel") ?? 0;
float weight = (float?)record.GetField("Weight") ?? 1.0f;
bool isRare = (bool?)record.GetField("IsRare") ?? false;

// Parse conditions as ParsedCondition list
var conditionList = record.GetField("Conditions") as List<ParsedCondition>;
```

### File Format Detection

The importer automatically detects format based on:

1. **Explicit extension** in filename (`.csv`, `.json`)
2. **Content inspection** (JSON files start with `{` or `[`)
3. **Default to CSV** if ambiguous

**Unsupported formats log a warning and return empty list.**

## Parser Behavior (For Developers)

### CSV Parser (`SchemaDrivenCsvParser`)

**Parsing:**
- Supports quoted commas, escaped quotes (`""`), multiline quoted fields
- Handles both LF and CRLF line endings
- First non-empty record is used as the header row
- Header column lookup is **case-insensitive**

**Validation:**
- Logs a warning for each schema column missing from the header
- For each row:
  - Row is invalid if a required column is missing from header
  - Row is invalid if any required cell is empty or whitespace-only
  - Invalid rows are skipped with a `Skipping row ...` warning

**Example:**
```csv
PlayerLevel,HasSword,Gold
10,true,500
5,,100
```
If `HasSword` is required, row 2 is skipped (empty value).

### JSON Parser (`SchemaDrivenJsonParser`)

**Parsing:**
- Supports root object (`{...}`) or root array (`[{...}, ...]`)
- Property lookup is **case-insensitive**
- Any other root format logs warning and returns no records

**Validation:**
- For each object:
  - Object is invalid if a required key is missing
  - Object is invalid if any required value is `null`, empty, or whitespace-only
  - Invalid objects are skipped with a `Skipping item ...` warning

**Example:**
```json
[
  {
    "PlayerLevel": 10,
    "HasSword": true,
    "Gold": 500
  },
  {
    "PlayerLevel": 5,
    "HasSword": null,
    "Gold": 100
  }
]
```
If `HasSword` is required, object 2 is skipped (null value).

### JSON Nested Object Flattening

If your JSON has nested structures, the parser automatically flattens them so a flat schema can still bind the data.

**Example input:**
```json
{
  "ItemID": "sword_rare",
  "Left": {
    "Choice": {
      "Answer": "Use sword"
    }
  }
}
```

**Generated aliases for schema binding:**
- Full path: `Left_Choice_Answer`
- Parent+leaf shortcuts: `Left_Answer`
- Numbered variants: `Left_Attribute1`, `Left_Attribute2`

This allows you to reference nested data without changing your schema structure.

## Type Conversion

When SchemaImporter processes a field, it converts the raw value to the specified type. Here's what happens:

### String

- **CSV:** Value is trimmed of leading/trailing whitespace
- **JSON:** Raw value is used as-is (can be `null`)
- **Behavior:** No conversion needed, just passed through

**Example:**
```
CSV: "  hello  " → "hello"
JSON: "hello" → "hello"
JSON: null → null
```

### Int

- Uses invariant-culture parse (ignores local culture settings)
- Invalid values log warning and default to `0`

**Example:**
```
CSV: "42" → 42
CSV: "abc" → 0 (warning logged)
JSON: 42 → 42
JSON: "not_a_number" → 0 (warning logged)
```

### Float

- Uses invariant-culture parse
- Invalid values log warning and default to `0f`

**Example:**
```
CSV: "3.14" → 3.14
CSV: "abc" → 0f (warning logged)
JSON: 3.14 → 3.14
JSON: "3.14" → 3.14 (will parse strings too)
```

### Bool

- Accepts `true`/`false` (case-insensitive)
- Accepts `1`/`0` (common in data files)
- Invalid values log warning and default to `false`

**Example:**
```
CSV: "true" → true
CSV: "1" → true
CSV: "yes" → false (warning logged)
JSON: true → true
JSON: 1 → true
JSON: "false" → false
```

### ConditionList

- Parsed by `ConditionParserUtility`
- Empty/null/whitespace source yields an empty list
- **See Condition Syntax section below**

**Example:**
```
"playerLevel >= 10 && hasGold == 1" → List<ParsedCondition> with 2 conditions
"" → empty list
null → empty list
```

## Condition Syntax (`ConditionParserUtility`)

Conditions are text expressions that get parsed into `ParsedCondition` objects. These are typically used in your CSV/JSON data to define when options are available.

**Example:** `"playerLevel >= 10 && hasWeapon == 1"`

### Operators

Supported comparison operators:

| Operator | Meaning |
|----------|---------|
| `==` | Equal to |
| `!=` | Not equal to |
| `>` | Greater than |
| `<` | Less than |
| `>=` | Greater than or equal to |
| `<=` | Less than or equal to |

**Examples:**
```
playerLevel >= 10     (playerLevel is at least 10)
health != 0           (health is not zero)
gold < 100            (gold is less than 100)
```

### Connectors (AND/OR Logic)

Multiple conditions can be combined with connectors:

| Connector | Meaning |
|-----------|---------|
| `&&`, `&`, `and`, `;` | AND (all conditions must be true) |
| `\|\|`, `\|`, `or` | OR (any condition can be true) |

**Examples:**
```
playerLevel >= 10 && hasWeapon == 1        (AND style 1)
playerLevel >= 10 & hasWeapon == 1         (AND style 2)
playerLevel >= 10 and hasWeapon == 1       (AND style 3)
playerLevel >= 10 ; hasWeapon == 1         (AND style 4)

isEnemy == 1 || isDead == 1                (OR style 1)
isEnemy == 1 | isDead == 1                 (OR style 2)
isEnemy == 1 or isDead == 1                (OR style 3)
```

### Shortcuts

For common patterns, you can use shorthand:

| Shorthand | Expands To |
|-----------|------------|
| `fieldName` | `fieldName == 1` |
| `!fieldName` | `fieldName != 1` |

**Examples:**
```
hasWeapon                   → hasWeapon == 1 (check if true/set)
isRare                      → isRare == 1
!isBroken                   → isBroken != 1 (check if false/not set)
!cursed                     → cursed != 1
```

### Literal Constants

You can also use boolean literals:

| Constant | Meaning |
|----------|---------|
| `TRUE`, `true` | Always true |
| `FALSE`, `false` | Always false |

**Examples:**
```
TRUE                        (always passes)
FALSE && otherCondition     (always fails)
health > 0 || TRUE          (always passes due to OR)
```

### Robustness

The parser is designed to be forgiving:

- **Malformed segments are skipped** - If one condition is invalid, others are kept
- **Valid segments around malformed ones are preserved** - Partial success is better than total failure
- **Connector metadata is assigned from the second parsed condition onward** - The first condition doesn't have a connector

**Examples:**
```
"playerLevel >= 10 && ??? && health > 5"
→ Keeps playerLevel >= 10 and health > 5, logs warning about ???

"playerLevel >= && health > 5"
→ Skips playerLevel comparison (malformed), keeps health > 5

"!@#$%"
→ Empty result, warning logged
```

## Troubleshooting & Best Practices

### Practical Notes

**Common Issues:**

| Problem | Likely Cause | Solution |
|---------|-------------|----------|
| Fewer records than expected | Required fields are being rejected | Check that all required columns exist and aren't empty |
| Unexpected `0`/`0f`/`false` values | Type conversion fallback after warning | Check the Import Log for parse warnings |
| Conditions not working as expected | Malformed operators or invalid variable names | Verify condition syntax, use the examples above |
| JSON nested data isn't binding | Schema aliases don't match nested structure | Use generated alias names (e.g., `Parent_Child_Property`) |

### Checklist:

- ✅ Use meaningful column names (e.g., "ItemID" not "ID")
- ✅ Keep boolean columns in lowercase (easier to read conditions)
- ✅ Test your data by creating a schema and checking for warnings in the Import Log
- ✅ Use CSV for simple data, JSON for complex nested structures
- ✅ Mark fields as Required only if they're truly needed
- ❌ Don't leave required cells empty - the row will be skipped
- ❌ Don't mix incompatible types in a column (e.g., "10" and "abc" for Int fields)

- ✅ Cache imported data rather than importing on every access
- ✅ Log records imported (check if warnings indicate rejection)
- ✅ Use null-coalescing when reading optional fields: `record.GetField("OptionalField") ?? defaultValue`
- ✅ Handle `null` returns from `ImportFromSchema` (can happen with missing files)
- ✅ Test with both CSV and JSON to ensure parser compatibility
- ❌ Don't assume all rows in the file become records (validation may skip some)
- ❌ Don't use culture-specific number formats in CSV - use invariant format

## Source of Truth (Tests)

Current behavior is covered by comprehensive unit and integration tests:

- `Assets/Tests/EditMode/SchemaImporter/SchemaDrivenCsvParserTests.cs` - CSV parsing edge cases
- `Assets/Tests/EditMode/SchemaImporter/SchemaDrivenJsonParserTests.cs` - JSON parsing edge cases
- `Assets/Tests/EditMode/SchemaImporter/DynamicDataImporterCsvIntegrationTests.cs` - End-to-end CSV import
- `Assets/Tests/EditMode/SchemaImporter/DynamicDataImporterJsonIntegrationTests.cs` - End-to-end JSON import

**Notable Covered Scenarios:**

- CSV quoting/escaping, multiline fields, blank row skipping, CRLF support
- JSON root object/array handling and invalid root rejection
- Optional vs required behavior, including whitespace-only required failures
- Parse-default behavior for invalid numbers/bools
- Condition operators/connectors/flags/literals and malformed segment tolerance
- Nested JSON aliases mapped to flat schema fields
- File format auto-detection and fallback behavior

If behavior is not as documented, check these tests first - they are the source of truth.
