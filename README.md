# Weighted Draw System

Weighted Draw System is a Unity package that helps you build data-driven random choice systems.

It includes two reusable modules:

- `ProbabilityEngine`: weighted selection with condition filtering
- `SchemaImporter`: schema-based CSV and JSON import into typed records

The importer and runtime now share condition semantics through `ConditionSemantics`, and built-in file routing uses internal adapters for `CsvDataParser` and `JsonDataParser`.

This repository contains the full Unity project plus the package source at `Packages/com.macmat01.weighted-draw-system`.

## What This Package Is For

Use this package when you want to:

- load gameplay tables from CSV or JSON
- validate and convert imported data with a schema
- evaluate conditional weighted choices at runtime

Typical use cases include loot tables, dialogue branching, encounter rolls, and event selection.

## Install

### Unity Package Manager (Git URL)

Because the `package.json` is inside a subfolder, use the Git URL with a `path` query:

```text
https://github.com/MacMat01/weighted-draw-system.git?path=/Packages/com.macmat01.weighted-draw-system
```

Optional: pin to a branch, tag, or commit with `#revision`:

```text
https://github.com/MacMat01/weighted-draw-system.git?path=/Packages/com.macmat01.weighted-draw-system#main
```

### Add Through `manifest.json`

```json
{
  "dependencies": {
    "com.macmat01.weighted-draw-system": "https://github.com/MacMat01/weighted-draw-system.git?path=/Packages/com.macmat01.weighted-draw-system"
  }
}
```

### Local Development

Keep the package in place at `Packages/com.macmat01.weighted-draw-system` inside this repository.

## Package Structure

- `Packages/com.macmat01.weighted-draw-system/package.json`
- `Packages/com.macmat01.weighted-draw-system/Runtime/ProbabilityEngine`
- `Packages/com.macmat01.weighted-draw-system/Runtime/SchemaImporter`
- `Packages/com.macmat01.weighted-draw-system/Tests/EditMode/ProbabilityEngine`
- `Packages/com.macmat01.weighted-draw-system/Tests/EditMode/SchemaImporter`
- `Packages/com.macmat01.weighted-draw-system/Documentation`

## Quick Workflow

1. Define a `DataSchemaSO` for your data format.
2. Import CSV or JSON rows with `DynamicDataImporter`.
3. Convert imported records into weighted choices.
4. Evaluate valid options with `ProbabilityEngine` or `RandomiserSystem`.

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

## Module Docs

- `Packages/com.macmat01.weighted-draw-system/README.md`
- `Packages/com.macmat01.weighted-draw-system/Documentation/ProbabilityEngine.md`
- `Packages/com.macmat01.weighted-draw-system/Documentation/SchemaImporter.md`

## Testing

Run Unity Edit Mode tests in:

- `Packages/com.macmat01.weighted-draw-system/Tests/EditMode/ProbabilityEngine`
- `Packages/com.macmat01.weighted-draw-system/Tests/EditMode/SchemaImporter`


## License

See `LICENSE`.
