 # Weighted Draw System (com.macmat01.weighted-draw-system)

This package provides a small, well-scoped toolset for authoring CSV-driven data tables in the Unity Editor
and performing weighted random selection at runtime. It is intentionally split into a lightweight runtime
surface (no editor dependencies) and an editor authoring/compile bridge.

Goals:
- Keep runtime usage strongly-typed and dependency-free from editor assemblies.
- Let designers author content in CSVs and let the editor compile those CSVs into ScriptableObject assets.
- Provide a deterministic-friendly weighted draw engine with pluggable eligibility and RNG.

Contents overview
- Runtime (Runtime/): CSV parser, authoring base types, condition evaluator, and the weighted draw engine.
- Editor (Editor/): import bridge and `CsvImportService` which compiles CSV text into compiled ScriptableObjects.
- Tests (Tests/EditMode/): unit tests that demonstrate parser, compiler and engine behaviors.

Key components (short)
- `Authoring.CsvDataSourceSO<T>`: base ScriptableObject for authoring CSV-backed tables (editor-only behavior via `OnValidate`).
- `Authoring.IRowDeserializer<T>`: implement this in your project to map CSV rows (Dictionary<string,string>) to your typed row `T`.
- `Authoring.CsvRowCompiler<T>`: runs the CSV parser and calls your `IRowDeserializer<T>` for every row, returning `List<T>`.
- `Authoring.CompiledCsvTableSO<T>`: a ScriptableObject container that stores the compiled `List<T>` as a serializable asset.
- `Csv.RobustCsvParser`: CSV tokenizer that supports quotes, escaped quotes, multiline cells and legacy wrapped-record recovery.
- `Conditions.ConditionEvaluator` + `Conditions.IGameStateReader`: optional expression evaluator for enabling/guarding rows.
- `WeightedDraw.WeightedDrawEngine<TEntry,TContext>`: picks an entry from an IEnumerable<TEntry> using eligibility + weights.
- `WeightedDraw.IRandomValueProvider`: inject to make selection deterministic in tests.

How the authoring/compile pipeline works (detailed)
1) Create a concrete authoring ScriptableObject by inheriting `CsvDataSourceSO<T>` and implementing `GetDeserializer()` to
   return your `IRowDeserializer<T>` implementation. Example below.
2) Assign one or more `TextAsset` CSV sources to `SourceCsvFiles` on the authoring asset in the Editor.
3) `CsvDataSourceSO<T>.OnValidate()` runs in the Editor and:
   - computes a stable `SourceSignature` based on assigned `TextAsset` instance IDs,
   - re-discovers headers from the first row (row 0) of every source and merges them into `Columns` (case-insensitive),
   - optionally calls the editor import bridge to compile CSVs into a `CompiledCsvTableSO<T>` when `AutoCompileInEditor` is true.
4) The runtime-to-editor bridge `Authoring.EditorImportBridge` uses reflection to locate the editor assembly type
   `Editor.Import.CsvImportService` (assembly name `MacMat01.WeightedDrawSystem.Editor`) and calls its
   `CompileGeneric<T>(CsvDataSourceSO<T>)` method. This keeps runtime code free of direct editor references.
5) `Editor.Import.CsvImportService.CompileGeneric<T>` obtains your `IRowDeserializer<T>` via reflection, ensures a
   `CompiledCsvTableSO<T>` exists (creates one or tries to find a concrete derived type in the domain assemblies),
   compiles the CSV text sources with `CsvRowCompiler<T>`, writes rows into the compiled table and saves the assets.

CSV schema and compilation rules
- Headers are read from the first row of each CSV file and are merged (case-insensitive) into `CsvDataSourceSO<T>.Columns`.
- `CsvColumnDefinition.IsRequired` marks a column as required; `CsvRowCompiler<T>` will throw an `InvalidOperationException`
  during compile if a required column is missing from any CSV source.
- Each data row is transformed into a `Dictionary<string,string>` by `CsvRowCompiler<T>` (keys are header names, lookup is
  case-insensitive when deserializing). Your `IRowDeserializer<T>.DeserializeRow(...)` maps those strings into typed values.
- If `DeserializeRow` returns `null` for a row, that row is skipped. Throw exceptions from your deserializer to fail fast.
- Multiple `TextAsset` sources are processed in order and concatenated when compiling.

Compiled asset behavior
- `CompiledCsvTableSO<T>` stores compiled rows in a private serialized `List<T>` and exposes them via `IReadOnlyList<T> Rows`.
- When `CsvImportService` creates a new `CompiledCsvTableSO<T>` and the authoring asset is already a saved asset in the
  project, the compiled table is attached as a sub-asset (`AssetDatabase.AddObjectToAsset`) and assigned to the
  `CsvDataSourceSO<T>.CompiledTable` property.

Using `WeightedDrawEngine<TEntry,TContext>`
Constructor:
- `new WeightedDrawEngine<TEntry,TContext>(Func<TEntry,TContext,bool> isEligible, Func<TEntry,float> weightSelector, IRandomValueProvider rng = null)`

Behavior highlights:
- `GetValidEntries(...)` returns entries that pass `isEligible(entry, context)`.
- `Draw(...)` selects among the valid entries. Negative weights are treated as `0`.
- If total positive weight is <= 0, a uniform random index is chosen among valid entries.
- If weights sum to > 0, a float target in [0, totalWeight) is drawn and a cumulative scan picks the first entry where
  `target <= cumulative`.
- If the loop completes without selecting (edge-case due to an out-of-range random float), the last valid entry is returned
  as a safety fallback.

Randomness and determinism
- By default the engine uses `WeightedDraw.UnityRandomValueProvider.Shared` which calls `UnityEngine.Random.Range`.
- For deterministic tests inject your own `IRandomValueProvider` implementation.

Using condition expressions (optional)
- `Conditions.ConditionEvaluator` parses and evaluates simple boolean expressions such as `Finance>10 && HasKeycard`.
- Expression connectors supported: `&&`, `||`, `&`, `|`, `;`, `and`, `or` (case-insensitive where applicable).
- Comparison operators: `==`, `!=`, `>`, `<`, `>=`, `<=`.
- Negation: prefix a flag with `!` (e.g. `!HasFlag`).
- `ConditionEvaluator` depends on an `IGameStateReader` implementation; `DictionaryGameStateReader` converts common
  primitives (float, double, int, long, bool and numeric/boolean strings) into float values and performs case-insensitive
  key lookup.

Extending the package — step-by-step examples

1) Define a row type

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

2) Implement `IRowDeserializer<T>` in your own project

```csharp
using System.Collections.Generic;
using Authoring;

public sealed class LootRowDeserializer : IRowDeserializer<LootRow>
{
	public LootRow DeserializeRow(IReadOnlyDictionary<string,string> rowData, int rowNumber)
	{
		// rowNumber is 1-based and indicates the source CSV row index (useful for error messages)
		if (rowData == null) return null;

		string id = rowData.TryGetValue("ItemId", out var s) ? s : string.Empty;
		int weight = int.TryParse(rowData.TryGetValue("Weight", out var w) ? w : null, out var parsed) ? parsed : 0;
		bool unlocked = bool.TryParse(rowData.TryGetValue("IsUnlocked", out var u) ? u : null, out var b) && b;

		return new LootRow { ItemId = id, Weight = weight, IsUnlocked = unlocked };
	}
}
```

3) Create a concrete `CompiledCsvTableSO<T>` (recommended though optional)

```csharp
using Authoring;

public sealed class LootCompiledTableSO : CompiledCsvTableSO<LootRow> { }
```

4) Create an authoring asset type

```csharp
using Authoring;
using UnityEngine;

[CreateAssetMenu(menuName = "Weighted Draw System/Loot Table")]
public sealed class LootTableAuthoringSO : CsvDataSourceSO<LootRow>
{
	protected override IRowDeserializer<LootRow> GetDeserializer() => new LootRowDeserializer();
}
```

5) Author CSV files and assign them to `SourceCsvFiles` on the created authoring asset. Enable `AutoCompileInEditor`
   if you want OnValidate to automatically run the compile bridge.

6) At runtime consume the compiled rows and the engine

```csharp
var compiled = lootAuthoring.CompiledTable?.Rows;
var engine = new WeightedDraw.WeightedDrawEngine<LootRow, object>(
	(row, _) => row != null && row.IsUnlocked,
	row => row.Weight);
LootRow pick = engine.Draw(compiled, null);
```

Testing and sample coverage
- See `Tests/EditMode/WeightedDrawEngineTests.cs` for draw engine edge cases (zero/negative weights, uniform fallback,
  and deterministic random provider examples).
- See `Tests/EditMode/RobustCsvParserTests.cs` for CSV parsing behaviors including quoted cells and legacy wrapped-records.

Implementation notes and gotchas
- `CsvDataSourceSO<T>.OnValidate()` only resyncs `Columns` when the `SourceSignature` (based on `TextAsset` instance IDs)
  changes — that avoids repeated work when the assets are unchanged.
- Header discovery uses the first row only — ensure your CSV files include a header row.
- `CsvRowCompiler<T>` validates required columns per-source and will throw on missing required columns.
- The editor bridge uses reflection; if you move or rename the editor assembly/type you need to update the
  hard-coded type name in `Authoring.EditorImportBridge`.

Contributing and further work
- The package is intentionally small; add new features by keeping runtime APIs free of UnityEditor references and
  putting editor-only helpers under the `Editor/` folder.
- If you need JSON importers or richer schema features, implement them as separate compile-time steps that produce
  `CompiledCsvTableSO<T>` instances so runtime code remains clean.

License
- See the repository `LICENSE` file.

