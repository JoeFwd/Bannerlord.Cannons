using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.CustomBattle;
using TOR_Core.AbilitySystem;

namespace TOR_Core.Extensions
{
    public static class AgentExtensions
    {
        public static bool IsAbilityUser(this Agent agent)
        {
            return agent.GetAttributes().Contains("AbilityUser");
        }

        public static bool IsEngineerCompanion(this Agent agent)
        {
            return agent.HasAttribute("EngineerCompanion");
        }

        public static bool IsArtilleryCrew(this Agent agent)
        {
            return agent.HasAttribute("ArtilleryCrew");
        }

        public static bool CanPlaceArtillery(this Agent agent)
        {
            return agent.GetAttributes().Contains("CanPlaceArtillery");
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

        public static Hero GetHero(this Agent agent)
        {
            if (agent == null) return null;
            
            if (agent.Character == null) return null;
            Hero hero = null;
            if (Game.Current.GameType is Campaign)
            {
                var character = agent.Character as CharacterObject;
                if (character != null && character.IsHero) hero = character.HeroObject;
            }
            return hero;
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

        public static int GetPlaceableArtilleryCount(this Agent agent)
        {
            int count = 0;
            if (agent.CanPlaceArtillery() || agent.IsEngineerCompanion())
            {
                if (Game.Current.GameType is Campaign && agent.GetHero() != null)
                {
                    count = agent.GetHero().GetPlaceableArtilleryCount();
                }
                else if (Game.Current.GameType is CustomGame)
                {
                    count = 5;
                }
            }
            return count;
        }

        public static List<string> GetSelectedAbilities(this Agent agent)
        {
            var hero = agent.GetHero();
            var character = agent.Character;
            var abilities = new List<string>();
            if (hero != null)
            {
                abilities.AddRange(hero.GetExtendedInfo().SelectedAbilities);
            }
            else if (character != null)
            {
                abilities.AddRange(agent.Character.GetAbilities());
            }
            
            return abilities;
        }

        public static List<string> GetAttributes(this Agent agent)
        {
            List<string> result = new List<string>();
            var hero = agent.GetHero();
            var character = agent.Character;
            if (hero != null)
            {
                var info = hero.GetExtendedInfo();
                if(info != null && info.AllAttributes.Count > 0)
                {
                    foreach(var attribute in info.AllAttributes)
                    {
                        if (!result.Contains(attribute)) result.Add(attribute);
                    }
                }
            }
            else if (character != null)
            {
                foreach(var attribute in character.GetAttributes())
                {
                    if (!result.Contains(attribute)) result.Add(attribute);
                }
            }

            return result;
        }

        public static bool HasAttribute(this Agent agent, string attributeName)
        {
            return agent.GetAttributes().Contains(attributeName);
        }
    }
}
