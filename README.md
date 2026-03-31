# Weighted-Draw-System

A Unity 6 project with two reusable, domain-agnostic modules:
- `ProbabilityEngine`: condition-aware weighted selection
- `SchemaImporter`: schema-driven CSV/JSON data import

This repository is structured as a Unity project.

## What This Repo Contains

- `Assets/Scripts/ProbabilityEngine`: generic runtime selection engine
- `Assets/Scripts/SchemaImporter`: schema-based parsing/import pipeline
- `Assets/Tests/EditMode/ProbabilityEngine`: edit mode tests for probability logic
- `Assets/Tests/EditMode/SchemaImporter`: edit mode tests for importer/parser behavior

## Core Features

### ProbabilityEngine

Use `ProbabilityEngine<TState, TValue>` when you need to:
- filter options by conditions against runtime state
- select one valid option by weighted randomness
- keep logic generic across gameplay contexts (loot, AI, events, etc.)

Detailed documentation:
- `Assets/Scripts/ProbabilityEngine/Docs/ProbabilityEngine.md`

### SchemaImporter

Use `SchemaImporter` when you need to:
- import CSV/JSON into typed `DataRecord` rows
- enforce required fields and type conversion via schema
- parse condition expressions from data files into structured condition objects

Detailed documentation:
- `Assets/Scripts/SchemaImporter/Docs/SchemaImporter.md`

## Start Here

1. Define your data table/file (CSV or JSON).
2. Create and configure a `DataSchemaSO` asset.
3. Validate imported records and fix warnings.
4. Tune weights/conditions for balancing.

Read first:
- `Assets/Scripts/SchemaImporter/Docs/SchemaImporter.md`
- `Assets/Scripts/ProbabilityEngine/Docs/ProbabilityEngine.md`

Then:

1. Integrate `SchemaImporter` to load and validate data.
2. Map imported records into `ProbabilityItem<TState, TValue>`.
3. Implement `IGameState` and custom `ICondition<TState>` where needed.
4. Evaluate with `GetValidChoices(...)` and `EvaluateRandom(...)`.

Read first:
- `Assets/Scripts/SchemaImporter/Docs/SchemaImporter.md`
- `Assets/Scripts/ProbabilityEngine/Docs/ProbabilityEngine.md`

## Minimal Workflow Example

```csharp
// 1) Import data from schema
List<DataRecord> records = DynamicDataImporter.ImportFromSchema(schema);

// 2) Build randomiser facade (schema-driven condition + weight columns)
var randomiser = new RandomiserSystem(records, schema);

// 3) Evaluate using runtime context
var context = new Dictionary<string, object>
{
    { "playerLevel", 10 },
    { "hasWeapon", 1 }
};

DataRecord selected = randomiser.EvaluateRandom(context);
```

## Project Requirements

- Unity 6 (project currently targets `6000.4` in existing docs)

## Testing

Use Unity Test Runner (Edit Mode) for module verification:
- `Assets/Tests/EditMode/ProbabilityEngine`
- `Assets/Tests/EditMode/SchemaImporter`

The feature docs also reference the test files that define current expected behavior.

## Contributing

When changing behavior:
- update or add tests in `Assets/Tests/EditMode/...`
- update feature docs in:
  - `Assets/Scripts/ProbabilityEngine/Docs/ProbabilityEngine.md`
  - `Assets/Scripts/SchemaImporter/Docs/SchemaImporter.md`
- keep top-level `README.md` as a navigation and onboarding guide

## License

See `LICENSE`.
