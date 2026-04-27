# Weighted Draw System

`Weighted Draw System` is a Unity UPM package for building data-driven weighted selection systems from CSV files.

It is intentionally split into a generic runtime and editor authoring pipeline so you can:

- keep gameplay code strongly typed
- let designers maintain content in spreadsheet-style CSVs
- compile CSV content to `ScriptableObject` assets in the editor
- run weighted draws with your own eligibility rules and game context

## What is included

### Runtime (`Runtime/`)

- `Csv/RobustCsvParser`: robust CSV tokenizer (quotes, escaped quotes, multiline-safe handling)
- `Authoring/CsvRowCompiler<T>`: transforms parsed rows into your domain type via `IRowDeserializer<T>`
- `Authoring/CompiledCsvTableSO<T>`: serialized container for compiled rows
- `WeightedDraw/WeightedDrawEngine<TEntry, TContext>`: filtering + weighted random selection
- `Conditions/ConditionEvaluator`: optional expression evaluator for conditions like `A>10&&FlagX`

### Editor (`Editor/`)

- `Authoring/CsvDataSourceSO<T>` authoring flow via `OnValidate()`
- `Editor/Import/CsvImportService` compiler bridge
- auto-sync of discovered columns from attached CSV files
- auto-compile of source CSV text into compiled table data

## End-to-end pipeline: how the tool works

1. You create a custom authoring asset by inheriting from `CsvDataSourceSO<T>`.
2. You assign one or more CSV `TextAsset` files to `SourceCsvFiles`.
3. `OnValidate()` runs in editor:
   - computes a source signature
   - re-discovers headers across sources and syncs `Columns`
   - optionally compiles if `AutoCompileInEditor` is enabled
4. `CsvImportService.CompileGeneric<T>` executes through the editor bridge:
   - obtains your `IRowDeserializer<T>` by reflection
   - ensures `CompiledTable` exists (or creates one)
   - compiles all rows from all source CSV files through `CsvRowCompiler<T>`
   - writes rows into `CompiledCsvTableSO<T>`
5. At runtime, your gameplay system reads `CompiledTable.Rows` and calls `WeightedDrawEngine<TEntry, TContext>.Draw(...)`.

## CSV import lifecycle details

### Header and schema handling

- headers are discovered from row `0` of each CSV source
- discovered headers are merged into `Columns` case-insensitively
- `CsvColumnDefinition.IsRequired` controls strict required-column validation during compile
- missing required columns throw an exception in `CsvRowCompiler<T>`

### Row compilation behavior

- each row becomes a `Dictionary<string, string>` keyed by header
- your deserializer converts dictionary values to typed fields
- if your deserializer returns `null`, that row is skipped
- multiple source CSV files are concatenated in compile order

### Compilation output

- compiled rows are saved into `CompiledCsvTableSO<T>`
- when the authoring asset exists in the AssetDatabase, compiled table can be stored as a sub-asset
- `EditorUtility.SetDirty` and `AssetDatabase.SaveAssets` persist editor changes

## Practical tutorial

This tutorial creates a small weighted loot table from CSV, compiles it in editor, then draws an entry in runtime.

### 1) Define your row model

```csharp
using System;

[Serializable]
public sealed class LootRow
{
	public string ItemId;
	public int Weight;
	public bool IsUnlocked;
}
```

### 2) Implement row deserialization

```csharp
using System.Collections.Generic;
using Authoring;

public sealed class LootRowDeserializer : IRowDeserializer<LootRow>
{
	public LootRow DeserializeRow(IReadOnlyDictionary<string, string> rowData, int rowNumber)
	{
		_ = rowNumber;

		return new LootRow
		{
			ItemId = Get(rowData, "ItemId"),
			Weight = ParseInt(rowData, "Weight"),
			IsUnlocked = ParseBool(rowData, "IsUnlocked")
		};
	}

	private static string Get(IReadOnlyDictionary<string, string> rowData, string key)
	{
		return rowData != null && rowData.TryGetValue(key, out string value) ? value : string.Empty;
	}

	private static int ParseInt(IReadOnlyDictionary<string, string> rowData, string key)
	{
		return int.TryParse(Get(rowData, key), out int value) ? value : 0;
	}

	private static bool ParseBool(IReadOnlyDictionary<string, string> rowData, string key)
	{
		return bool.TryParse(Get(rowData, key), out bool value) && value;
	}
}
```

### 3) Create a concrete compiled table type (recommended)

```csharp
using Authoring;

public sealed class LootCompiledTableSO : CompiledCsvTableSO<LootRow>
{
}
```

### 4) Create your authoring asset type

```csharp
using Authoring;
using UnityEngine;

[CreateAssetMenu(menuName = "Weighted Draw System/Loot Table")]
public sealed class LootTableAuthoringSO : CsvDataSourceSO<LootRow>
{
	protected override IRowDeserializer<LootRow> GetDeserializer()
	{
		return new LootRowDeserializer();
	}
}
```

### 5) Create the CSV

Example `TextAsset` contents:

```csv
ItemId,Weight,IsUnlocked
potion_small,60,true
shield_basic,30,true
legendary_core,10,false
```

### 6) Configure in Unity Editor

1. Create `LootTableAuthoringSO` asset.
2. Assign your CSV `TextAsset` to `SourceCsvFiles`.
3. (Recommended) assign `CompiledTable` to an instance of `LootCompiledTableSO`.
4. Ensure `AutoCompileInEditor` is enabled.
5. Confirm columns are discovered under `Columns`.
6. Save asset; compile runs via `OnValidate()`.

### 7) Draw entries at runtime

```csharp
using WeightedDraw;

public static class LootSelector
{
	public static LootRow DrawUnlocked(LootTableAuthoringSO authoring)
	{
		var rows = authoring.CompiledTable != null ? authoring.CompiledTable.Rows : null;

		var engine = new WeightedDrawEngine<LootRow, object>(
			static (row, _) => row != null && row.IsUnlocked,
			static row => row.Weight);

		return engine.Draw(rows, null);
	}
}
```

## Using condition expressions (optional)

If your CSV stores condition strings like `Finance>40&&HasKeycard`, evaluate them through `ConditionEvaluator`:

- use `DictionaryGameStateReader` to expose game-state values
- call `ConditionEvaluator.Evaluate(expression, gameStateReader)`
- plug that boolean into `WeightedDrawEngine` eligibility

Example supported operators and connectors:

- comparison: `==`, `!=`, `>`, `<`, `>=`, `<=`
- connectors: `&&`, `||`, `&`, `;`, `and`, `or`
- flags: `HasDiedOnce` (truthy if value exists and is non-zero)
- negation: `!HasDiedOnce`

## Current tool vs legacy `_Old` tool

Everything under `_Old/` is deprecated and kept for migration only.

### Architecture changes

- **Old**: split into `ProbabilityEngine` + `SchemaImporter` with dynamic `DataRecord` dictionaries.
- **Current**: unified typed pipeline (`CsvDataSourceSO<T>` -> `CsvRowCompiler<T>` -> `CompiledCsvTableSO<T>` -> `WeightedDrawEngine<TEntry, TContext>`).

### Data model changes

- **Old**: `DataSchemaSO` + `ColumnDefinition` + `DataRecord` object fields.
- **Current**: your own strongly typed row class `T` and explicit deserializer logic.

### Selection engine changes

- **Old**: `ProbabilityEngine<TState, TValue>` + `ProbabilityItem<TState, TValue>`.
- **Current**: `WeightedDrawEngine<TEntry, TContext>` with injected eligibility and weight selectors.

### Condition handling changes

- **Old**: condition parsing utilities in `_Old/Runtime/SchemaImporter/Parsers`.
- **Current**: lightweight `ConditionEvaluator` + `IGameStateReader` in `Runtime/Conditions`.

### Source format scope

- **Old**: CSV and JSON importers (`DynamicDataImporter`).
- **Current**: CSV-first core flow focused on robust typed import and compile.

### Why this refactor

- less reflection-heavy runtime usage
- stronger compile-time safety through typed rows
- cleaner separation of authoring/import/runtime concerns
- easier testing with deterministic `IRandomValueProvider`

## Where to look for examples

- integration tests: `Tests/EditMode/WeightedDrawSystemIntegrationTests.cs`
- legacy references: `_Old/Tests/EditMode`

## Packaging notes

- this file is the package `documentationUrl` target
- package code is designed to be consumed as immutable UPM content
- game-specific schemas, deserializers, and gameplay glue should live in your project code

