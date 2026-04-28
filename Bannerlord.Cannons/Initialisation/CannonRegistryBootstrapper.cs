using System.Collections.Generic;
using System.Linq;
using Bannerlord.Cannons.Domain;
using Bannerlord.Cannons.Infrastructure;
using Bannerlord.Cannons.Infrastructure.Registry;
using Bannerlord.Cannons.Integration.Mission.Spawn;

namespace Bannerlord.Cannons.Initialisation
{
    public class CannonRegistryBootstrapper
    {
        private readonly ICannonConfigurationReader _reader;
        private readonly ValidateCannonsUseCase _validator;
        private readonly CannonRegistry _registry;

        public CannonRegistryBootstrapper(
            ICannonConfigurationReader reader,
            ValidateCannonsUseCase validator,
            CannonRegistry registry)
        {
            _reader = reader;
            _validator = validator;
            _registry = registry;
        }

        public IReadOnlyList<Cannon> Bootstrap()
        {
            var validCannons = _validator.GetValidCannons(_reader.LoadCannons()).ToList();
            foreach (var cannon in validCannons)
            {
                var dynamicType = CannonTypeEmitter.EmitCannonType(cannon.Id);
                _registry.RegisterCannon(cannon, new GenericCannonFactory(cannon.Id, dynamicType));
            }
            return validCannons;
        }
    }
}
