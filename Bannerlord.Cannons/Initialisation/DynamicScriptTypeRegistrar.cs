using System.Collections.Generic;
using Bannerlord.Cannons.DI;
using Bannerlord.Cannons.Domain;
using Bannerlord.Cannons.Infrastructure.Registry;
using Bannerlord.Cannons.Integration.Mission.Spawn;
using TaleWorlds.DotNet;

namespace Bannerlord.Cannons.Initialisation
{
    public class DynamicScriptTypeRegistrar
    {
        public void Register(IEnumerable<Cannon> cannons)
        {
            var scriptTypes = new Dictionary<string, System.Type>();
            var registry = CannonsRuntimeServices.GetRequiredService<ICannonRegistry>();

            foreach (var cannon in cannons)
            {
                scriptTypes[CannonTypeEmitter.GetTypeName(cannon.Id)] =
                    registry.GetFactory(cannon.Id)!.CannonScriptType;
            }

            var spawnerType = SpawnerTypeEmitter.EmitSpawnerType();
            scriptTypes[spawnerType.Name] = spawnerType;
            Managed.AddTypes(scriptTypes);
        }
    }
}
