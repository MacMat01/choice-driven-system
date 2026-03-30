using System.Collections.Generic;
using ProbabilisticEngine.Interfaces;

namespace ProbabilisticEngine.Runtime
{
    public class GameState : IGameState
    {
        private Dictionary<string, int> _resources = new(); // TO DO - MAKE RESOURCES GENERAL
        private HashSet<string> _flags = new();
        private Dictionary<string, string> _context = new();

        public int GetResource(string key) => _resources.TryGetValue(key, out var v) ? v : 0;
        public void SetResource(string key, int value) => _resources[key] = value;

        public bool HasFlag(string flag) => _flags.Contains(flag);
        public void SetFlag(string flag) => _flags.Add(flag);

        public string GetContext(string key) => _context.TryGetValue(key, out var v) ? v : null;
        public void SetContext(string key, string value) => _context[key] = value;

        public bool IsOnCooldown(string optionId, int turns) => false; // TO DO
        public bool HasSeenOption => false; // placeholder
    }
}