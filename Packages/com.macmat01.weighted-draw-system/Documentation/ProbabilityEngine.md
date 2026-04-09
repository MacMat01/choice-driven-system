# ProbabilityEngine

## What This Module Does

`ProbabilityEngine` selects one outcome from a list of options by combining:

1. condition checks against game state
2. weighted probability across valid options
3. random selection

Use it when outcomes are not always available and you want designer-friendly weight tuning.

Typical uses include loot drops, encounter events, dialogue lines, and AI behavior choices.

## Main Types

### `ProbabilityEngine<TState, TValue>`

Core class that filters and selects items.

- `GetValidChoices(TState state)` returns options whose conditions pass
- `EvaluateRandom(TState state)` returns one weighted random valid item, or `null`

### `ProbabilityItem<TState, TValue>`

Represents one selectable option.

- `Id`: unique identifier
- `BaseWeight`: selection weight (higher means more likely)
- `Value`: the payload you want to return
- `Conditions`: optional list of `ICondition<TState>`

### `IGameState`

Marker interface for your game state type.

### `ICondition<TState>`

Condition interface used by each item.

```csharp
public interface ICondition<in TState> where TState : IGameState
{
    bool Evaluate(TState state);
}
```

## Quick Start

### 1) Define your state and conditions

```csharp
public sealed class CombatState : IGameState
{
    public int PlayerLevel { get; set; }
    public bool IsBossFight { get; set; }
}

public sealed class MinLevelCondition : ICondition<CombatState>
{
    public int RequiredLevel { get; set; }

    public bool Evaluate(CombatState state)
    {
        return state.PlayerLevel >= RequiredLevel;
    }
}
```

### 2) Build a weighted pool

```csharp
var options = new List<ProbabilityItem<CombatState, string>>
{
    new ProbabilityItem<CombatState, string>
    {
        Id = "common_attack",
        BaseWeight = 1.0f,
        Value = "Attack",
        Conditions = null
    },
    new ProbabilityItem<CombatState, string>
    {
        Id = "special_attack",
        BaseWeight = 0.25f,
        Value = "SpecialAttack",
        Conditions = new List<ICondition<CombatState>>
        {
            new MinLevelCondition { RequiredLevel = 10 }
        }
    }
};
```

### 3) Evaluate

```csharp
var engine = new ProbabilityEngine<CombatState, string>(options);

var state = new CombatState
{
    PlayerLevel = 12,
    IsBossFight = false
};

List<ProbabilityItem<CombatState, string>> valid = engine.GetValidChoices(state);
ProbabilityItem<CombatState, string> picked = engine.EvaluateRandom(state);

if (picked != null)
{
    Debug.Log($"Selected: {picked.Id} ({picked.Value})");
}
```

## Behavior Notes

- If no options are valid, `EvaluateRandom(...)` returns `null`.
- Negative weights are treated as zero.
- If all valid weights are zero, selection falls back to uniform random across valid options.
- Multiple conditions on one item use AND logic (all must pass).

## Using Imported Data (`RandomiserSystem`)

If you import records through `SchemaImporter`, `RandomiserSystem` gives you dictionary-based evaluation without creating custom `TState` classes.

```csharp
List<DataRecord> records = DynamicDataImporter.ImportFromSchema(schema);

var randomiser = new RandomiserSystem(
    records,
    schema,
    conditionColumnName: "Conditions",
    weightColumnName: "Weight"
);

var context = new Dictionary<string, object>
{
    { "playerLevel", 10 },
    { "hasWeapon", 1 }
};

DataRecord selected = randomiser.EvaluateRandom(context);
```

## Best Practices

- Keep item IDs stable so analytics and saves can reference them.
- Prefer small, focused conditions rather than large monolithic ones.
- Keep weights in a narrow range for easier balancing.
- Add tests for edge cases: no valid choices, single valid choice, all zero weights.

## Related Files

- Runtime code: `Runtime/ProbabilityEngine`
- Tests: `Tests/EditMode/ProbabilityEngine`
- Schema integration docs: `Documentation/SchemaImporter.md`
