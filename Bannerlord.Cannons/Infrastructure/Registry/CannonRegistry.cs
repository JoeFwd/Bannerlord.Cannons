using System;
using System.Collections.Generic;
using System.Linq;
using Bannerlord.Cannons.Domain;

namespace Bannerlord.Cannons.Infrastructure.Registry
{
    public class CannonRegistry : ICannonRegistry
    {
        public static CannonRegistry Instance { get; private set; } = new CannonRegistry();

        internal static void Initialize(CannonRegistry registry)
        {
            Instance = registry;
        }

        private readonly List<Cannon> _cannons = new List<Cannon>();
        private readonly Dictionary<string, ICannonFactory> _factories = new Dictionary<string, ICannonFactory>();

        public void RegisterCannon(Cannon cannon, ICannonFactory factory)
        {
            _cannons.Add(cannon);
            _factories[cannon.Id] = factory;
        }

        public Cannon? GetCannon(string id)
        {
            return _cannons.FirstOrDefault(ct => ct.Id == id);
        }

        public Cannon? GetCannonByScript(Type scriptType)
        {
            var cannonId = _factories.FirstOrDefault(kvp => kvp.Value.CannonScriptType == scriptType).Key;
            return cannonId != null ? GetCannon(cannonId) : null;
        }

        public ICannonFactory? GetFactory(string id) =>
            _factories.TryGetValue(id, out var factory) ? factory : null;

        public IEnumerable<Cannon> GetAllCannons()
        {
            return _cannons;
        }
    }
}
