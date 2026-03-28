using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameState
{
    // --- Risorse generiche (stile Reigns, ma personalizzabili) ---
    // I designer possono aggiungere o rimuovere risorse senza toccare codice.
    public Dictionary<string, float> resources = new Dictionary<string, float>()
    {
        { "church", 50f },
        { "people", 50f },
        { "army", 50f },
        { "treasury", 50f }
    };

    // --- Turno corrente ---
    public int turn = 0;

    // --- Carte/eventi già mostrati ---
    public HashSet<string> seenCards = new HashSet<string>();

    // --- Storico delle decisioni ---
    [Serializable]
    public struct DecisionRecord
    {
        public string cardId;
        public string choiceId;
        public int turn;
    }

    public List<DecisionRecord> history = new List<DecisionRecord>();

    // --- Flag narrativi generici ---
    public Dictionary<string, bool> narrativeFlags = new Dictionary<string, bool>();

    // --- Variabili di contesto generiche ---
    public Dictionary<string, float> contextVars = new Dictionary<string, float>();


    // -------------------------------------------------------------
    // Utility Methods
    // -------------------------------------------------------------

    public void SetResource(string key, float value)
    {
        if (!resources.ContainsKey(key))
            resources[key] = value;
        else
            resources[key] = Mathf.Clamp(value, 0f, 100f);
    }

    public float GetResource(string key)
    {
        return resources.TryGetValue(key, out float v) ? v : 0f;
    }

    public void AddResourceDelta(string key, float delta)
    {
        if (!resources.ContainsKey(key))
            resources[key] = 50f; // default

        resources[key] = Mathf.Clamp(resources[key] + delta, 0f, 100f);
    }

    public void MarkCardSeen(string cardId)
    {
        seenCards.Add(cardId);
    }

    public void AddDecision(string cardId, string choiceId)
    {
        history.Add(new DecisionRecord
        {
            cardId = cardId,
            choiceId = choiceId,
            turn = turn
        });
    }

    public void SetFlag(string flag, bool value)
    {
        narrativeFlags[flag] = value;
    }

    public bool GetFlag(string flag)
    {
        return narrativeFlags.TryGetValue(flag, out bool v) && v;
    }

    public void SetContext(string key, float value)
    {
        contextVars[key] = value;
    }

    public float GetContext(string key, float defaultValue = 0f)
    {
        return contextVars.TryGetValue(key, out float v) ? v : defaultValue;
    }
}