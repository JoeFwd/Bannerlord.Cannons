using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TOR_Core.AbilitySystem;

namespace TOR_Core.Extensions
{
    public static class AgentExtensions
    {
        public static bool IsAbilityUser(this Agent agent)
        {
            return agent.Character?.StringId == "main_hero";
        }

        public static bool IsEngineerCompanion(this Agent agent)
        {
            return false;
        }

        public static bool IsArtilleryCrew(this Agent agent)
        {
            return true;
        }

        public static bool CanPlaceArtillery(this Agent agent)
        {
            return agent.Character?.StringId == "main_hero";
        }

        public static Ability GetAbility(this Agent agent, int abilityindex)
        {
            var abilitycomponent = agent.GetComponent<AbilityComponent>();
            if (abilitycomponent != null)
            {
                return abilitycomponent.GetAbility(abilityindex);
            }

            return null;
        }

        public static void SelectAbility(this Agent agent, int abilityindex)
        {
            var abilitycomponent = agent.GetComponent<AbilityComponent>();
            if (abilitycomponent != null)
            {
                abilitycomponent.SelectAbility(abilityindex);
            }
        }

        public static void SelectAbility(this Agent agent, Ability ability)
        {
            var abilitycomponent = agent.GetComponent<AbilityComponent>();
            if (abilitycomponent != null)
            {
                abilitycomponent.SelectAbility(ability);
            }
        }

        public static Ability GetCurrentAbility(this Agent agent)
        {
            var abilitycomponent = agent.GetComponent<AbilityComponent>();
            if (abilitycomponent != null)
            {
                return abilitycomponent.CurrentAbility;
            }
            else return null;
        }

        public static bool TryCastCurrentAbility(this Agent agent, out TextObject failureReason)
        {
            var abilitycomponent = agent.GetComponent<AbilityComponent>();

            if (abilitycomponent != null)
            {
                if (abilitycomponent.CurrentAbility != null) return abilitycomponent.CurrentAbility.TryCast(agent, out failureReason);
            }
            failureReason = new TextObject("{=tor_cast_fail_comp_null}Abilitycomponent is null!");
            return false;
        }
    }
}
