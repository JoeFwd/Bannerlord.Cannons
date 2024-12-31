using System.Collections.Generic;
using TOR_Core.Extensions.ExtendedInfoSystem;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.SaveSystem;

namespace TOR_Core.SaveGameSystem
{
    public class TORSaveableTypeDefiner : SaveableTypeDefiner
    {
        public TORSaveableTypeDefiner() : base(98652987) { }

        protected override void DefineClassTypes()
        {
            AddClassDefinition(typeof(HeroExtendedInfo), 1);
            AddClassDefinition(typeof(MobilePartyExtendedInfo), 2);
        }

        protected override void DefineContainerDefinitions()
        {
            ConstructContainerDefinition(typeof(Dictionary<string, HeroExtendedInfo>));
            ConstructContainerDefinition(typeof(Dictionary<string, MobilePartyExtendedInfo>));
            ConstructContainerDefinition(typeof(Dictionary<string, List<string>>));
        }
    }
}
