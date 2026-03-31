# ProbabilityEngine Documentation

## Overview

The **ProbabilityEngine** is a generic, domain-agnostic system that selects outcomes from a pool of options based on **conditions**, **weights**, and **randomness**. It can power loot systems, NPC behavior, procedural generation, narrative choices, or any system where you need weighted random selection influenced by game state.

**Key Features:**
- ✅ Generic & type-safe (works with any data type)
- ✅ Condition-based filtering (options only valid if conditions are met)
- ✅ Weight-based probability (higher weight = more likely)
- ✅ State-aware evaluation (evaluates against your game state)
- ✅ Easy integration with data importers

## How It Works

1. **Filter**: Keep only options whose conditions match your game state
2. **Weight**: Each valid option has a probability weight
3. **Select**: Use weighted random selection to pick one option
4. **Return**: The selected option is returned (you decide what to do with it)

## Architecture

### Core Components

#### 1. **ProbabilityEngine<TState, TValue>** ⭐ Main Class
The generic probability engine that drives everything.

```csharp
public class ProbabilityEngine<TState, TValue>
    where TState : IGameState
{
    public List<ProbabilityItem<TState, TValue>> GetValidChoices(TState state)
    public ProbabilityItem<TState, TValue> EvaluateRandom(TState state)
}
```

**What it does:**
- Takes a collection of items and a game state
- Filters items: keeps only those whose conditions are met
- Performs weighted random selection on valid items
- Returns the selected item (with its data/value intact)

**Generic parameters:**
- `TState`: Your game state type (must implement `IGameState`)
- `TValue`: The data type of each option (can be anything: strings, objects, data records, etc.)

**Example usages:**
```csharp
// Selecting a loot drop
var engine = new ProbabilityEngine<GameState, LootDrop>(lootOptions);
var selectedLoot = engine.EvaluateRandom(currentGameState);

// Selecting an NPC response
var engine = new ProbabilityEngine<DialogueState, DialogueLine>(dialogueOptions);
var selectedLine = engine.EvaluateRandom(currentDialogueState);

// Selecting a procedural event
var engine = new ProbabilityEngine<WorldState, Event>(eventOptions);
var selectedEvent = engine.EvaluateRandom(currentWorldState);
```

#### 2. **ProbabilityItem<TState, TValue>** 
Represents a single option in your pool.

```csharp
public class ProbabilityItem<TState, TValue>
{
    public string Id;                              // Unique identifier
    public float BaseWeight;                       // Probability weight (higher = more likely)
    public TValue Value;                           // The actual data (loot, dialogue, etc.)
    public List<ICondition<TState>> Conditions;    // Must ALL be true for this item to be valid
    
    public bool AreConditionsMet(TState state)     // Check if this item can be selected
}
```

**What it represents:**
- A single selectable option with its associated data
- The weight determines how likely it is to be chosen
- Conditions gate whether it can be chosen at all

#### 3. **IGameState**
The interface your game state must implement.

```csharp
public interface IGameState
{
    // Your implementation decides what goes here
}
```

**What it is:**
- A marker interface that your game state must implement
- Allows the engine to work with any state type
- Your state class implements this and provides whatever properties/methods conditions need to evaluate

**Example:**
```csharp
public class MyGameState : IGameState
{
    public int PlayerHealth { get; set; }
    public bool HasSword { get; set; }
    public int Gold { get; set; }
    // ... any other state you need
}
```

#### 4. **ICondition<TState>**
The interface for condition logic.

```csharp
public interface ICondition<in TState> where TState : IGameState
{
    bool Evaluate(TState state);  // Returns true if this condition is satisfied
}
```

**What it does:**
- Evaluates whether an option should be available
- Your conditions implement this to check game state
- ALL conditions on an item must return true for it to be valid

### Game State

The engine is **agnostic about your game state structure**. You define:
- What properties your state has
- How conditions evaluate against that state

**Example game states:**
```csharp
// Loot system state
public class LootState : IGameState
{
    public int PlayerLevel { get; set; }
    public string CurrentBiome { get; set; }
    public bool IsBoss { get; set; }
}

// Narrative system state
public class DialogueState : IGameState
{
    public int PlayerReputation { get; set; }
    public HashSet<string> CompletedQuests { get; set; }
    public bool KnowsSecret { get; set; }
}

// Procedural world state
public class WorldState : IGameState
{
    public int DayCount { get; set; }
    public WeatherType CurrentWeather { get; set; }
    public float Population { get; set; }
}
```

## Extensibility

### Creating Custom Conditions

Since all options must implement `ICondition<TState>`, you create conditions by implementing this interface:

```csharp
public interface ICondition<in TState> where TState : IGameState
{
    bool Evaluate(TState state);
}
```

**Example conditions:**

```csharp
// Check if player has enough health
public class HealthThresholdCondition : ICondition<PlayerState>
{
    public int MinHealth { get; set; }
    
    public bool Evaluate(PlayerState state)
    {
        return state.PlayerHealth >= MinHealth;
    }
}

// Check if player has a specific item
public class HasItemCondition : ICondition<PlayerState>
{
    public string ItemId { get; set; }
    
    public bool Evaluate(PlayerState state)
    {
        return state.Inventory.Contains(ItemId);
    }
}

// Check time-based availability
public class TimeWindowCondition : ICondition<WorldState>
{
    public int StartHour { get; set; }
    public int EndHour { get; set; }
    
    public bool Evaluate(WorldState state)
    {
        int currentHour = state.CurrentTime.Hour;
        return currentHour >= StartHour && currentHour < EndHour;
    }
}

// Check multiple conditions (AND logic)
public class CompositeCondition : ICondition<GameState>
{
    public List<ICondition<GameState>> Conditions { get; set; }
    
    public bool Evaluate(GameState state)
    {
        return Conditions.All(c => c.Evaluate(state));
    }
}

// Check numeric comparison
public class NumericCondition : ICondition<WorldState>
{
    public string StatName { get; set; }
    public float ComparisonValue { get; set; }
    public ComparisonOperator Operator { get; set; }
    
    public bool Evaluate(WorldState state)
    {
        float statValue = state.GetStat(StatName);
        return Operator switch
        {
            ComparisonOperator.GreaterThan => statValue > ComparisonValue,
            ComparisonOperator.LessThan => statValue < ComparisonValue,
            ComparisonOperator.Equal => Mathf.Approximately(statValue, ComparisonValue),
            ComparisonOperator.GreaterOrEqual => statValue >= ComparisonValue,
            ComparisonOperator.LessOrEqual => statValue <= ComparisonValue,
            _ => false
        };
    }
}
```

**How to use in your pool:**

```csharp
var lootPool = new List<ProbabilityItem<LootState, Loot>>
{
    new ProbabilityItem<LootState, Loot>
    {
        Id = "common_drop",
        BaseWeight = 1.0f,
        Value = new Loot { Name = "Rusty Sword" },
        Conditions = null  // No conditions = always valid
    },
    new ProbabilityItem<LootState, Loot>
    {
        Id = "rare_drop",
        BaseWeight = 0.2f,
        Value = new Loot { Name = "Legendary Sword" },
        Conditions = new List<ICondition<LootState>>
        {
            new HealthThresholdCondition { MinHealth = 50 }
        }
    }
};

var engine = new ProbabilityEngine<LootState, Loot>(lootPool);
var selectedLoot = engine.EvaluateRandom(currentState);
```

## Data Structure

### ProbabilityItem<TState, TValue>

The fundamental unit of the system. Represents a single selectable option.

```csharp
public class ProbabilityItem<TState, TValue>
{
    public string Id;                              // Unique identifier
    public float BaseWeight;                       // Base probability weight
    public TValue Value;                           // Any data type (your game data)
    public List<ICondition<TState>> Conditions;    // Optional list of conditions
}
```

**For Designers:**
- **Id**: Give each option a meaningful name (e.g., "dragon_loot_rare")
- **BaseWeight**: Higher numbers = more likely to be picked
  - Weight 1.0 = default probability
  - Weight 2.0 = twice as likely as weight 1.0
  - Weight 0.5 = half as likely as weight 1.0
- **Value**: The actual game data (loot object, dialogue line, NPC response, etc.)
- **Conditions**: List of conditions that must ALL be true for this option to be available

**For Developers:**
- `TValue` can be any type: primitives, custom classes, scriptable objects, data records, etc.
- Null conditions = option is always valid
- Empty conditions list = option is always valid
- Multiple conditions must ALL evaluate to true (AND logic)

## How to Use It

### Step 1: Define Your Game State

Implement `IGameState` with whatever data your conditions need:

```csharp
public class GameState : IGameState
{
    public int Health { get; set; }
    public int Level { get; set; }
    public bool HasWeapon { get; set; }
    public Dictionary<string, int> Resources { get; set; }
}
```

### Step 2: Create Your Conditions

Implement `ICondition<TState>` for each rule you need:

```csharp
public class IsHighLevel : ICondition<GameState>
{
    public int RequiredLevel { get; set; }
    
    public bool Evaluate(GameState state)
    {
        return state.Level >= RequiredLevel;
    }
}

public class HasWeaponEquipped : ICondition<GameState>
{
    public bool Evaluate(GameState state)
    {
        return state.HasWeapon;
    }
}
```

### Step 3: Create Your Options

Build your pool of `ProbabilityItem<TState, TValue>`:

```csharp
// Example: Loot drop system
var lootOptions = new List<ProbabilityItem<GameState, Loot>>
{
    new ProbabilityItem<GameState, Loot>
    {
        Id = "common_drop",
        BaseWeight = 1.0f,
        Value = new Loot { Name = "Iron Sword", Rarity = "Common" },
        Conditions = null  // Always available
    },
    new ProbabilityItem<GameState, Loot>
    {
        Id = "rare_drop",
        BaseWeight = 0.3f,
        Value = new Loot { Name = "Dragon Blade", Rarity = "Rare" },
        Conditions = new List<ICondition<GameState>>
        {
            new IsHighLevel { RequiredLevel = 10 }
        }
    },
    new ProbabilityItem<GameState, Loot>
    {
        Id = "legendary_drop",
        BaseWeight = 0.05f,
        Value = new Loot { Name = "Excalibur", Rarity = "Legendary" },
        Conditions = new List<ICondition<GameState>>
        {
            new IsHighLevel { RequiredLevel = 20 },
            new HasWeaponEquipped()
        }
    }
};
```

### Step 4: Create the Engine

```csharp
var engine = new ProbabilityEngine<GameState, Loot>(lootOptions);
```

### Step 5: Evaluate When Needed

```csharp
GameState currentState = new GameState 
{ 
    Level = 15, 
    HasWeapon = true, 
    Health = 100 
};

// Get only valid options for current state
List<ProbabilityItem<GameState, Loot>> validDrops = 
    engine.GetValidChoices(currentState);

Console.WriteLine($"Valid drops: {validDrops.Count}");

// Pick one randomly based on weights
ProbabilityItem<GameState, Loot> selectedDrop = 
    engine.EvaluateRandom(currentState);

if (selectedDrop != null)
{
    Loot loot = selectedDrop.Value;
    Console.WriteLine($"You received: {loot.Name}");
}
```

## Example Scenario: Combat Encounter

**Setup:** An RPG where enemy AI decides its action based on game state.

**State:**
```csharp
public class CombatState : IGameState
{
    public int PlayerHealth { get; set; }
    public int EnemyHealth { get; set; }
    public bool PlayerHasShield { get; set; }
}
```

**Conditions:**
```csharp
public class PlayerHealthLow : ICondition<CombatState>
{
    public int Threshold { get; set; }
    public bool Evaluate(CombatState state) => state.PlayerHealth <= Threshold;
}

public class PlayerHasShield : ICondition<CombatState>
{
    public bool Evaluate(CombatState state) => state.PlayerHasShield;
}
```

**Actions (TValue = Action):**
```csharp
enum Action { Attack, Defense, Flee }

var enemyActions = new List<ProbabilityItem<CombatState, Action>>
{
    // Always aggressive if player weak
    new ProbabilityItem<CombatState, Action>
    {
        Id = "aggressive_attack",
        BaseWeight = 2.0f,
        Value = Action.Attack,
        Conditions = new List<ICondition<CombatState>>
        {
            new PlayerHealthLow { Threshold = 30 }
        }
    },
    
    // Defend if player has shield
    new ProbabilityItem<CombatState, Action>
    {
        Id = "defensive_stance",
        BaseWeight = 1.5f,
        Value = Action.Defense,
        Conditions = new List<ICondition<CombatState>>
        {
            new PlayerHasShield()
        }
    },
    
    // Flee as last resort
    new ProbabilityItem<CombatState, Action>
    {
        Id = "flee",
        BaseWeight = 0.1f,
        Value = Action.Flee,
        Conditions = null  // Always available (but very unlikely)
    }
};

var engine = new ProbabilityEngine<CombatState, Action>(enemyActions);
```

**Evaluation:**
```csharp
CombatState state = new CombatState 
{ 
    PlayerHealth = 20,      // Low!
    EnemyHealth = 50,
    PlayerHasShield = false 
};

var selectedAction = engine.EvaluateRandom(state);

// Result: Likely to be Action.Attack because PlayerHealthLow condition is met
// and "aggressive_attack" has weight 2.0
```

## How Weighted Selection Works

The engine uses `WeightedRandom.PickIndex()` to select options based on their weights:

```csharp
public static int PickIndex(List<float> weights)
{
    // Sum all weights
    float total = weights.Sum();
    
    // Pick a random value between 0 and total
    float r = Random.value * total;
    
    // Find which weight range the random value falls into
    for (int i = 0; i < weights.Count; i++)
    {
        if (r < weights[i]) return i;
        r -= weights[i];
    }
    
    return weights.Count - 1;
}
```

**Example:**
```csharp
var weights = new[] { 1.0f, 2.0f, 0.5f };  // Total = 3.5
// Probabilities:
//   Option 0: 1.0 / 3.5 ≈ 29%
//   Option 1: 2.0 / 3.5 ≈ 57%
//   Option 2: 0.5 / 3.5 ≈ 14%
```

**Edge Cases:**
- **No valid options**: `EvaluateRandom()` returns `null`
- **All weights are zero**: Falls back to uniform random selection
- **Negative weights**: Converted to 0

## Performance Characteristics

- **Time Complexity:**
  - `GetValidChoices()`: O(n × m) where n = item count, m = conditions per item
  - `EvaluateRandom()`: O(n × m + n log n) due to condition evaluation and weighted selection

- **Space Complexity:**
  - Engine instance: O(n) where n = item count
  - Per evaluation: O(n) for valid items list

**Optimization Tips:**
- Cache the engine instance if evaluating the same pool repeatedly
- Keep condition lists short (typically 1-3 conditions per item)
- Avoid expensive state lookups in condition evaluation
- Pre-sort items if you have a clear probability tier system

## RandomiserSystem: Data Integration

The `RandomiserSystem` class bridges imported data with the `ProbabilityEngine`:

```csharp
public sealed class RandomiserSystem
{
    public List<DataRecord> GetValidChoices(IReadOnlyDictionary<string, object> gameStateContext)
    public DataRecord EvaluateRandom(IReadOnlyDictionary<string, object> gameStateContext)
}
```

**Key Features:**
- Works with imported CSV/JSON data records
- Supports condition columns and weight columns
- Uses a dictionary-based game state for dynamic context
- Automatically builds conditions from parsed condition lists

**Usage:**
```csharp
// Load imported data
var dataRecords = importer.LoadRecords("loot_table.csv");

// Create randomiser system
var randomiser = new RandomiserSystem(
    dataRecords, 
    schema, 
    conditionColumnName: "Conditions",
    weightColumnName: "Weight"
);

// Evaluate with dynamic context
var context = new Dictionary<string, object>
{
    { "playerLevel", 10 },
    { "isBoss", false },
    { "hasLuckPotion", true }
};

var selectedRecord = randomiser.EvaluateRandom(context);
```

## Best Practices

### For Designers:
- ✅ Keep base weights between 0.1 and 10 for easier balancing
- ✅ Use meaningful IDs (e.g., "loot_rare_sword" not "option_5")
- ✅ Test conditions thoroughly—an option with an always-false condition is wasted
- ✅ Group related options into separate engines for clarity
- ❌ Don't create too many condition types—reuse existing ones

### For Developers:
- ✅ Keep game state implementations simple and focused
- ✅ Make conditions stateless (don't modify state during evaluation)
- ✅ Return `null` conditions list if option has no conditions (faster than empty list)
- ✅ Cache engines if used frequently
- ✅ Test edge cases: no valid options, all weights zero, single option
- ❌ Don't put complex logic in condition evaluation—pre-compute if possible

