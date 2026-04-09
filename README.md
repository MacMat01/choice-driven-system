# Weighted Draw System

A Unity 6 package and source repository containing two reusable, domain-agnostic modules:

- `ProbabilityEngine`: condition-aware weighted selection
- `SchemaImporter`: schema-driven CSV/JSON data import

The package lives under `Packages/com.macmat01.weighted-draw-system` and is structured for Unity Package Manager, Git-based installation, and local development.

## Package Layout

- `Packages/com.macmat01.weighted-draw-system/Runtime/ProbabilityEngine`: runtime weighted-selection engine
- `Packages/com.macmat01.weighted-draw-system/Runtime/SchemaImporter`: runtime import/parsing pipeline
- `Packages/com.macmat01.weighted-draw-system/Tests/EditMode/ProbabilityEngine`: Edit Mode tests for the probability engine
- `Packages/com.macmat01.weighted-draw-system/Tests/EditMode/SchemaImporter`: Edit Mode tests for the importer and parsers
- `Packages/com.macmat01.weighted-draw-system/Tests/EditMode/Fixtures`: test-only schema/data assets
- `Packages/com.macmat01.weighted-draw-system/Documentation~`: package documentation

## Install Options

### Local package

Open the Unity project and keep the package under `Packages/com.macmat01.weighted-draw-system`.

### Git-based install

Use the package folder as a Git dependency in Unity Package Manager.

### Package Manager

The package manifest is `Packages/com.macmat01.weighted-draw-system/package.json`.

## Core Modules

### `ProbabilityEngine`

Use `ProbabilityEngine<TState, TValue>` when you need to:

- filter options by conditions against runtime state
- select one valid option by weighted randomness
- keep logic generic across gameplay contexts such as loot, AI, and events

Detailed documentation:

- `Packages/com.macmat01.weighted-draw-system/Documentation~/ProbabilityEngine.md`

### `SchemaImporter`

Use `SchemaImporter` when you need to:

- import CSV/JSON into typed `DataRecord` rows
- enforce required fields and type conversion via schema
- parse condition expressions from data files into structured condition objects

Detailed documentation:

- `Packages/com.macmat01.weighted-draw-system/Documentation~/SchemaImporter.md`

## Quick Start

1. Open the Unity project with the package installed.
2. Create and configure a `DataSchemaSO` asset.
3. Load the example fixtures from `Packages/com.macmat01.weighted-draw-system/Tests/EditMode/Fixtures` when validating tests.
4. Tune weights and conditions for balancing.

Then:

1. Integrate `SchemaImporter` to load and validate data.
2. Map imported records into `ProbabilityItem<TState, TValue>`.
3. Implement `IGameState` and custom `ICondition<TState>` where needed.
4. Evaluate with `GetValidChoices(...)` and `EvaluateRandom(...)`.

## Minimal Workflow Example

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

## Testing

Run the Unity Test Runner in Edit Mode and target:

- `Packages/com.macmat01.weighted-draw-system/Tests/EditMode/ProbabilityEngine`
- `Packages/com.macmat01.weighted-draw-system/Tests/EditMode/SchemaImporter`

The fixture assets used by these tests live in `Packages/com.macmat01.weighted-draw-system/Tests/EditMode/Fixtures`.

## Contributing

When changing behavior:

- update or add tests in `Packages/com.macmat01.weighted-draw-system/Tests/EditMode/...`
- update package docs in `Packages/com.macmat01.weighted-draw-system/Documentation~/`
- keep the package README and this root README aligned with the package structure

## License

See `LICENSE`.
