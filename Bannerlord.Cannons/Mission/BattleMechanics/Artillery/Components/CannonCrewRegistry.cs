using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.BattleMechanics.Artillery.Components
{
    /// <summary>
    /// Mission-scoped registry that tracks which agent is locked to which cannon seat.
    /// Held as a static field on <see cref="ArtilleryRangedSiegeWeapon"/> so all cannon
    /// instances share one registry per mission without needing DI — avoids pulling
    /// <see cref="TaleWorlds.MountAndBlade.UsableMachine"/> into the Application or Domain layers.
    /// </summary>
    internal sealed class CannonCrewRegistry
    {
        private readonly struct Entry
        {
            public readonly UsableMachine Machine;
            public readonly int SeatId;

            public Entry(UsableMachine machine, int seatId)
            {
                Machine = machine;
                SeatId = seatId;
            }
        }

        private readonly Dictionary<int, Entry> _registry = new Dictionary<int, Entry>();

        public void Claim(int agentId, UsableMachine machine, int seatId)
        {
            _registry[agentId] = new Entry(machine, seatId);
        }

        public void Release(int agentId)
        {
            _registry.Remove(agentId);
        }

        public bool IsOwnedBy(int agentId, UsableMachine machine, out int seatId)
        {
            seatId = -1;
            if (!_registry.TryGetValue(agentId, out var entry))
                return false;
            if (entry.Machine != machine)
                return false;
            seatId = entry.SeatId;
            return true;
        }

        public bool IsOwnedByOther(int agentId, UsableMachine machine)
        {
            if (!_registry.TryGetValue(agentId, out var entry))
                return false;
            return entry.Machine != machine;
        }

        public void ReleaseAllFor(UsableMachine machine)
        {
            var toRemove = new List<int>();
            foreach (var kv in _registry)
            {
                if (kv.Value.Machine == machine)
                    toRemove.Add(kv.Key);
            }
            foreach (var id in toRemove)
                _registry.Remove(id);
        }
    }
}
