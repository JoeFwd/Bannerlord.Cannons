using System.Collections.Generic;
using Bannerlord.Cannons.Logging;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem.Crosshairs;

namespace TOR_Core.AbilitySystem
{
    public class AbilityComponent : AgentComponent
    {
        private static readonly ILogger Logger = new ConsoleLoggerFactory().CreateLogger<AbilityComponent>(); 
        
        private Ability _currentAbility = null;
        private readonly List<Ability> _knownAbilitySystem = new List<Ability>();
        public bool LastCastWasQuickCast;
        public List<Ability> KnownAbilitySystem { get => _knownAbilitySystem; }
        

        public AbilityComponent(Agent agent) : base(agent)
        {
            var ability1 = (ItemBoundAbility)AbilityFactory.CreateNew("GreatCannonSpawner");
            if (ability1 != null)
            {
                ability1.OnCastStart += OnCastStart;
                ability1.OnCastComplete += OnCastComplete;
                ability1.SetChargeNum(1);
                _knownAbilitySystem.Add(ability1);
            }

            var ability2 = (ItemBoundAbility)AbilityFactory.CreateNew("MortarSpawner");
            if (ability2 != null)
            {
                ability2.OnCastStart += OnCastStart;
                ability2.OnCastComplete += OnCastComplete;
                ability2.SetChargeNum(2);
                _knownAbilitySystem.Add(ability2);
            }
            var ability3 = (ItemBoundAbility)AbilityFactory.CreateNew("FieldTrebuchetSpawner");
            if (ability3 != null)
            {
                ability3.OnCastStart += OnCastStart;
                ability3.OnCastComplete += OnCastComplete;
                ability3.SetChargeNum(2);
                _knownAbilitySystem.Add(ability3);
            }

            if (_knownAbilitySystem.Count > 0)
            {
                SelectAbility(0);
            }
        }

        private void OnCastStart(Ability ability)
        {
            var manager = Mission.Current.GetMissionBehavior<AbilityManagerMissionLogic>();
            if (manager != null)
            {
                manager.OnCastStart(ability, Agent);
            }
        }

        private void OnCastComplete(Ability ability)
        {
            var manager = Mission.Current.GetMissionBehavior<AbilityManagerMissionLogic>();
            if (manager != null)
            {
                manager.OnCastComplete(ability, Agent);
            }
        }

        public void InitializeCrosshairs()
        {
            foreach (var ability in KnownAbilitySystem)
            {
                AbilityCrosshair crosshair = AbilityFactory.InitializeCrosshair(ability.Template);
                ability.SetCrosshair(crosshair);
            }
        }
        
        public void SelectAbility(Ability ability)
        {
            if(KnownAbilitySystem.Contains(ability)) CurrentAbility = ability;
        }

        public void SelectAbility(int index)
        {
            if (_knownAbilitySystem.Count > 0)
            {
                CurrentAbility = _knownAbilitySystem[index];
            }
        }

        public List<AbilityTemplate> GetKnownAbilityTemplates()
        {
            return _knownAbilitySystem.ConvertAll(ability => ability.Template);
        }

        public Ability GetAbility(int index)
        {
            if (_knownAbilitySystem.Count > 0 && index >= 0)
            {
                return _knownAbilitySystem[index % _knownAbilitySystem.Count];
            }

            return null;
        }
        
        public int GetCurrentAbilityIndex()
        {
            for (var index = 0; index < _knownAbilitySystem.Count; index++)
            {
                var ability = _knownAbilitySystem[index];
                if (ability == CurrentAbility)
                    return index;
            }

            return 0;
        }

        public override void OnTickAsAI(float dt)
        {
            base.OnTickAsAI(dt);
            foreach(var ability in _knownAbilitySystem)
            {
                if (ability.IsActivationPending) ability.ActivateAbility(Agent);
            }
        }

        public Ability CurrentAbility
        {
            get => _currentAbility;
            set
            {
                _currentAbility = value;
            }
        }
    }
}