using System;
using Bannerlord.Cannons.Infrastructure.Registry;

namespace Bannerlord.Cannons.Integration.Mission.Spawn
{
    public class GenericCannonFactory : ICannonFactory
    {
        private readonly string _cannonId;
        private readonly Type _scriptType;

        public GenericCannonFactory(string cannonId, Type scriptType)
        {
            if (cannonId == null) throw new ArgumentNullException(nameof(cannonId));
            if (scriptType == null) throw new ArgumentNullException(nameof(scriptType));

            if (!scriptType.IsSubclassOf(typeof(GenericCannon)))
                throw new ArgumentException($"Script type {scriptType.FullName} must be a subclass of GenericCannon", nameof(scriptType));

            _cannonId = cannonId;
            _scriptType = scriptType;
        }

        public Type CannonScriptType => _scriptType;

        public SpawnableArtilleryRangedSiegeWeapon CreateCannon()
        {
            try
            {
                var instance = Activator.CreateInstance(_scriptType);
                if (instance is not GenericCannon cannon)
                    throw new InvalidOperationException($"Failed to create instance of type {_scriptType.FullName}. Instance is not a GenericCannon.");
                return cannon;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create cannon instance of type {_scriptType.FullName} for cannon '{_cannonId}'", ex);
            }
        }
    }
}
