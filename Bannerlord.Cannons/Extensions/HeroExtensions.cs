using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.Extensions.ExtendedInfoSystem;

namespace TOR_Core.Extensions
{
    public static class HeroExtensions
    {
        public static int GetPlaceableArtilleryCount(this Hero hero)
        {
            // TODO: refactor redundancy with AgentExtension
            int count = 0;
            if (hero.CanPlaceArtillery() || hero.IsEngineerCompanion() && Hero.MainHero.CanPlaceArtillery())
            {
                var engineering = hero.GetSkillValue(DefaultSkills.Engineering);
                count = (int)Math.Truncate((decimal)engineering / 50);
                if (hero != Hero.MainHero && count == 0) count = 1; //Ensure AI lords can place at least 1 piece.
            }
            return count;
        }

        public static bool CanPlaceArtillery(this Hero hero)
        {
            return hero.HasAttribute("CanPlaceArtillery");
        }

        public static bool IsEngineerCompanion(this Hero hero)
        {
            return hero.HasAttribute("EngineerCompanion");
        }

        public static HeroExtendedInfo GetExtendedInfo(this Hero hero)
        {
            return ExtendedInfoManager.Instance.GetHeroInfoFor(hero.GetInfoKey());
        }

        public static string GetInfoKey(this Hero hero)
        {
            return hero.StringId;
        }

        public static void AddAttribute(this Hero hero, string attribute)
        {
            var info = hero.GetExtendedInfo();
            if (info != null && !info.AllAttributes.Contains(attribute))
            {
                info.AcquiredAttributes.Add(attribute);
            }
        }

        public static void AddAbility(this Hero hero, string ability)
        {
            var info = hero.GetExtendedInfo();
            if (info != null && !info.AllAbilities.Contains(ability))
            {
                info.AcquiredAbilities.Add(ability);
            }
        }

        public static bool HasAttribute(this Hero hero, string attribute)
        {
            if (hero.GetExtendedInfo() != null)
            {
                return hero.GetExtendedInfo().AllAttributes.Contains(attribute);
            }
            else return false;
        }
    }
}
