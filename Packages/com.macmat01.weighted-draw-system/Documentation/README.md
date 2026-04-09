# Weighted Draw System

`com.macmat01.weighted-draw-system` provides a small, reusable toolkit for data-driven random choice systems in Unity.

It combines:

- `ProbabilityEngine`: weighted random selection with condition filtering
- `SchemaImporter`: schema-based import of CSV and JSON into typed records

## When To Use It

Use this package when you need to:

- author gameplay tables outside code (CSV or JSON)
- validate required fields and parse values into strongly typed data
- select outcomes by weight only when runtime conditions are valid

Common scenarios: loot tables, procedural events, branching narrative choices, and AI decision pools.

## Runtime Modules

- `Runtime/ProbabilityEngine`
- `Runtime/SchemaImporter`

## Basic Example

```csharp
// 1) Import validated data from a schema-bound source file
List<DataRecord> records = DynamicDataImporter.ImportFromSchema(schema);

// 2) Evaluate weighted choices against runtime context
var randomiser = new RandomiserSystem(records, schema);

var context = new Dictionary<string, object>
{
    { "playerLevel", 12 },
    { "hasWeapon", 1 }
};

DataRecord selected = randomiser.EvaluateRandom(context);
```

## Documentation

- `Documentation/ProbabilityEngine.md`
- `Documentation/SchemaImporter.md`

## Tests

Edit Mode tests are included in:

- `Tests/EditMode/ProbabilityEngine`
- `Tests/EditMode/SchemaImporter`

## Repository And Installation

Repository:

- `https://github.com/MacMat01/weighted-draw-system`

Install from Git via Unity Package Manager:

```text
https://github.com/MacMat01/weighted-draw-system.git?path=/Packages/com.macmat01.weighted-draw-system
```

