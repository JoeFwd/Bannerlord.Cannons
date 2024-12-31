using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TOR_Core.Extensions.ExtendedInfoSystem;

namespace TOR_Core.Extensions
{
    public static class MobilePartyExtensions
    {
        public static MobilePartyExtendedInfo GetPartyInfo(this MobileParty party)
        {
            return ExtendedInfoManager.Instance.GetPartyInfoFor(party.StringId);
        }

        public static List<ItemRosterElement> GetArtilleryItems(this MobileParty party)
        {
            List<ItemRosterElement> list = [.. party.ItemRoster.Where(x => x.EquipmentElement.Item.StringId.Contains("artillery")).ToList()];
            return list;
        }

        public static int GetMaxNumberOfArtillery(this MobileParty party)
        {
            if (party == MobileParty.MainParty)
            {
                if (party.LeaderHero != null)
                {
                    var engineers = party.GetMemberHeroes();

                    var highestEngineer=  TaleWorlds.Core.Extensions.MaxBy(engineers, x => x.GetSkillValue(DefaultSkills.Engineering));
                    var engineering = highestEngineer.GetSkillValue(DefaultSkills.Engineering);
                    return (int)Math.Truncate((decimal)engineering / 50);
                }
                else return 0;
            }
            else if (party.IsLordParty)
            {
                return 3;
            }
            else return 0;
        }

        public static List<Hero> GetMemberHeroes(this MobileParty party)
        {
            List<Hero> heroes = [];
            var roster = party.MemberRoster?.GetTroopRoster();
            if (roster != null)
            {
                foreach (var member in roster)
                {
                    if (member.Character?.HeroObject != null)
                    {
                        heroes.Add(member.Character.HeroObject);
                    }
                }
            }
            return heroes;
        }
    }
}
