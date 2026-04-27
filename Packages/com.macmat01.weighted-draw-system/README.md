# Weighted Draw System

`Weighted Draw System` is a Unity UPM package for building data-driven weighted selection systems from CSV files.

It is intentionally split into a generic runtime and editor authoring pipeline so you can:

- keep gameplay code strongly typed
- let designers maintain content in spreadsheet-style CSVs
- compile CSV content to `ScriptableObject` assets in the editor
- run weighted draws with your own eligibility rules and game context

## What is included

### Runtime (`Runtime/`)

- `Authoring/CsvDataSourceSO<T>` authoring flow via `OnValidate()`
- `Csv/RobustCsvParser`: robust CSV tokenizer (quotes, escaped quotes, multiline-safe handling)
- `Authoring/CsvRowCompiler<T>`: transforms parsed rows into your domain type via `IRowDeserializer<T>`
- `Authoring/CompiledCsvTableSO<T>`: serialized container for compiled rows
- `WeightedDraw/WeightedDrawEngine<TEntry, TContext>`: filtering + weighted random selection
- `Conditions/ConditionEvaluator`: optional expression evaluator for conditions like `A>10&&FlagX`

### Editor (`Editor/`)

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

## WeightedDrawEngine guide

The engine is intentionally generic. You provide three things:

- `isEligible`: `Func<TEntry, TContext, bool>` (which entries are allowed)
- `weightSelector`: `Func<TEntry, float>` (how much chance each eligible entry gets)
- `IRandomValueProvider` (optional, for deterministic tests or custom RNG)

### How eligibility works

`WeightedDrawEngine<TEntry, TContext>` validates entries by calling your `isEligible(entry, context)` predicate in `GetValidEntries(...)`.

- if no eligible entries exist, `Draw(...)` returns `default`
- only eligible entries participate in random selection
- negative weights are clamped to `0`
- if total effective weight is `<= 0`, the engine falls back to uniform random among eligible entries

Probability rule (when total weight is positive):

- `P(entry) = max(0, weight(entry)) / Sum(max(0, weight(all eligible entries)))`

### Basic WeightedDrawEngine setup

```csharp
using WeightedDraw;

var engine = new WeightedDrawEngine<LootRow, object>(
	static (row, _) => row != null && row.IsUnlocked,
	static row => row.Weight);

LootRow selected = engine.Draw(authoring.CompiledTable.Rows, null);
```

### Context-driven filtering tutorial

Use `TContext` to pass game-state needed for eligibility checks.

```csharp
public sealed class CardDrawContext
{
	public int CurrentYear;
	public HashSet<int> BlockedCardIds;
}

var engine = new WeightedDrawEngine<CardRow, CardDrawContext>(
	static (row, ctx) =>
		row != null &&
		row.IsDrawable &&
		row.YearUnlock <= ctx.CurrentYear &&
		(ctx.BlockedCardIds == null || !ctx.BlockedCardIds.Contains(row.CardId)),
	static row => row.Weight);

CardRow selected = engine.Draw(rows, context);
```

### Condition-expression integration tutorial

If your CSV has `Pre_Conditions`, evaluate them in the eligibility predicate.

```csharp
using System;
using Conditions;

IConditionEvaluator evaluator = new ConditionEvaluator();
IGameStateReader gameState = new DictionaryGameStateReader(new Dictionary<string, object>
{
	["Finance"] = 55,
	["HasDiedOnce"] = true
});

var engine = new WeightedDrawEngine<CardRow, object>(
	(row, _) => row != null && evaluator.Evaluate(row.PreConditions, gameState),
	static row => row.Weight);
```

### Deterministic testing and custom randomness

For tests or special RNG behavior, inject a custom `IRandomValueProvider`.

```csharp
public sealed class FixedRandom : IRandomValueProvider
{
	public float NextFloat(float minInclusive, float maxExclusive) => minInclusive;
	public int NextInt(int minInclusive, int maxExclusive) => minInclusive;
}

var engine = new WeightedDrawEngine<LootRow, object>(
	static (_, _) => true,
	static row => row.Weight,
	new FixedRandom());
```

See `Tests/EditMode/WeightedDrawEngineTests.cs` for practical boundary and fallback examples.

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

## Game state reader details

`DictionaryGameStateReader` is the default adapter used with `ConditionEvaluator` when your game state is dictionary-based.

Behavior in `TryGetValue(key, out value)`:

- key lookup is case-insensitive
- unsupported keys return `false`
- values are converted to `float` when possible

Supported value conversions:

- `float`, `double`, `int`, `long`
- `bool` (`true` -> `1`, `false` -> `0`)
- numeric strings parsed with invariant culture (`"12.5"`)
- boolean strings (`"true"`, `"false"`)

Unsupported or null values return `false`.

If your game state is not dictionary-based (ECS, service layer, save model), implement your own `IGameStateReader` and pass it to `ConditionEvaluator`.

```csharp
using Conditions;

public sealed class PlayerStateReader : IGameStateReader
{
	private readonly PlayerState state;

	public PlayerStateReader(PlayerState state)
	{
		this.state = state;
	}

	public bool TryGetValue(string key, out float value)
	{
		value = 0f;
		if (state == null || string.IsNullOrWhiteSpace(key))
		{
			return false;
		}

		if (string.Equals(key, "Finance", StringComparison.OrdinalIgnoreCase))
		{
			value = state.Finance;
			return true;
		}

		if (string.Equals(key, "HasDiedOnce", StringComparison.OrdinalIgnoreCase))
		{
			value = state.HasDiedOnce ? 1f : 0f;
			return true;
		}

		return false;
	}
}
```

## WeightedDrawEngine edge behaviors

`WeightedDrawEngine` has a few important edge semantics:

- entry order matters when random target lands exactly on a boundary (`target <= cumulative`)
- negative weights are treated as `0`
- if all eligible weights are `<= 0`, selection becomes uniform over eligible entries
- if random provider returns out-of-range float, engine returns the last valid entry as safety fallback

These are covered in `Tests/EditMode/WeightedDrawEngineTests.cs`.

## RobustCsvParser compatibility notes

`RobustCsvParser` supports:

- quoted fields with commas
- escaped quotes (`""`)
- multiline quoted fields
- `LF` and `CRLF` line endings

Compatibility mode:

- legacy wrapped-record lines like `"Id,Name,Weight"` are normalized and reparsed
- this helps migration from `_Old` CSV exports that wrapped entire records

See `Tests/EditMode/RobustCsvParserTests.cs` for concrete coverage.

## Editor import bridge behavior

`CsvDataSourceSO<T>.OnValidate()` compiles through a reflection bridge to editor-only code.

- runtime side calls `EditorImportBridge`
- editor side resolves `Editor.Import.CsvImportService`
- missing type/method now logs explicit warnings instead of silent no-op
- invocation failures log an explicit error with type context

This is intentional to keep runtime assembly independent from direct editor assembly references.

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

