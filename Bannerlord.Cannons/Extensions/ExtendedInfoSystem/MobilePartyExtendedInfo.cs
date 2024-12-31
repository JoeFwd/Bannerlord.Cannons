using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace TOR_Core.Extensions.ExtendedInfoSystem
{
    public class MobilePartyExtendedInfo
    {
        [SaveableField(2)] public Dictionary<string, List<string>> TroopAttributes = [];
        
        public void AddTroopAttribute(CharacterObject troop, string attribute)
        {
            TroopAttributes ??= [];
            if (!TroopAttributes.TryGetValue(troop.StringId, out var entryList))
            {
                var list = new List<string>
                {
                    attribute
                };
                TroopAttributes.Add(troop.StringId, list);
            }
            else
            {
                entryList.Add(attribute);
                TroopAttributes[troop.StringId] = entryList;
            }
        }
    }
}