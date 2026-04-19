using System.Collections.Generic;
using System.Linq;
using Bannerlord.Cannons.Domain;
using Bannerlord.Cannons.Infrastructure;
using Bannerlord.Cannons.Infrastructure.Registry;
using Bannerlord.Cannons.Integration.Mission.Spawn;
using Bannerlord.Cannons.Logging;

namespace Bannerlord.Cannons.Initialisation
{
    public class CannonRegistryBootstrapper
    {
        public IReadOnlyList<Cannon> Bootstrap()
        {
            var loggerFactory = new ConsoleLoggerFactory();
            var reader = new XmlCannonConfigurationReader(loggerFactory);
            var validator = new ValidateCannonsUseCase(loggerFactory);
            var validCannons = validator.GetValidCannons(reader.LoadCannons()).ToList();

            var registry = new CannonRegistry();
            foreach (var cannon in validCannons)
            {
                var dynamicType = CannonTypeEmitter.EmitCannonType(cannon.Id);
                registry.RegisterCannon(cannon, new GenericCannonFactory(cannon.Id, dynamicType));
            }

            CannonRegistry.Initialize(registry);
            return validCannons;
        }
    }
}
