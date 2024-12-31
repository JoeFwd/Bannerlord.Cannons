using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Bannerlord.Cannons.Logging;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.ObjectSystem;
using TOR_Core.Utilities;

namespace TOR_Core.Extensions.ExtendedInfoSystem
{
    public class ExtendedInfoManager : CampaignBehaviorBase
    {
        private static readonly ILogger Logger = new ConsoleLoggerFactory().CreateLogger<ExtendedInfoManager>();
        
        private static Dictionary<string, CharacterExtendedInfo> _characterInfos = [];
        private Dictionary<string, HeroExtendedInfo> _heroInfos = [];
        private Dictionary<string, MobilePartyExtendedInfo> _partyInfos = [];
        private static ExtendedInfoManager _instance;

        public static ExtendedInfoManager Instance => _instance;

        public ExtendedInfoManager() => _instance = this;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionStart);
            CampaignEvents.OnNewGameCreatedPartialFollowUpEvent.AddNonSerializedListener(this, OnNewGameCreatedPartialFollowUpEnd);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, DailyTick);
            CampaignEvents.HeroCreated.AddNonSerializedListener(this, OnHeroCreated);
            CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
            CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, OnPartyCreated);
            CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnPartyDestroyed);
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            CampaignEvents.PlayerUpgradedTroopsEvent.AddNonSerializedListener(this, TroopUpgraded);
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, BattleEnd);
            CampaignEvents.OnTroopRecruitedEvent.AddNonSerializedListener(this, TroopRecruited);
        }

        private void TroopRecruited(Hero hero, Settlement arg2, Hero arg3, CharacterObject arg4, int arg5)
        {
            if (hero == null) return;
            if (hero.PartyBelongedTo.Party != null)
            {
                ValidatePartyInfos(hero.PartyBelongedTo);
            }
        }

        private void BattleEnd(MapEvent obj)
        {
            var parties = obj.PartiesOnSide(obj.PlayerSide);

            foreach (var party in parties)
            {
                if (party.Party.MobileParty == null) continue;
                
                ValidatePartyInfos(party.Party.MobileParty);
            }
        }

        private void TroopUpgraded(CharacterObject from, CharacterObject to, int count)
        {
            ValidatePartyInfos(MobileParty.MainParty);
        }

        private void DailyTick()
        {
            foreach (var entry in _partyInfos)
            {
                var party = Campaign.Current.LordParties.FirstOrDefault(x => x.StringId == entry.Key);
                
                ValidatePartyInfos(party);
            }
        }
        
        public void ValidatePartyInfos(MobileParty party)
        {
            if (!_partyInfos.TryGetValue(party.StringId, out var partyInfo))
            {
                return;
            }
            
            if (partyInfo.TroopAttributes == null)
            {
                partyInfo.TroopAttributes = [];
                return;
            }

            var roster = party.MemberRoster.GetTroopRoster();

            foreach (var troopAttribute in partyInfo.TroopAttributes.Keys.Reverse())
            {
                if (roster.All(x => x.Character.StringId != troopAttribute))
                {
                    partyInfo.TroopAttributes.Remove(troopAttribute);
                }
            }

            foreach (var element in roster.Where(element => !partyInfo.TroopAttributes.ContainsKey(element.Character.StringId)))
            {
                partyInfo.TroopAttributes.Add(element.Character.StringId, []);
            }
        }

        public static CharacterExtendedInfo GetCharacterInfoFor(string id)
        {
            if (_characterInfos.TryGetValue(id, out CharacterExtendedInfo value))
            {
                return value;
            }
            return null;
        }

        public HeroExtendedInfo GetHeroInfoFor(string id)
        {
            if (_heroInfos.TryGetValue(id, out HeroExtendedInfo value))
            {
                return value;
            }
            return null;
        }

        public MobilePartyExtendedInfo GetPartyInfoFor(string id)
        {
            if (_partyInfos.TryGetValue(id, out MobilePartyExtendedInfo value))
            {
                return value;
            }
            return null;
        }

        private void OnNewGameCreated(CampaignGameStarter starter)
        {
            if (_characterInfos.Count > 0) _characterInfos.Clear();
            TryLoadCharacters(out _characterInfos);
        }

        private void OnSessionStart(CampaignGameStarter obj)
        {
            if (_characterInfos.Count > 0) _characterInfos.Clear();
            TryLoadCharacters(out _characterInfos);
            EnsurePartyInfos();
            HideVanillaUnitsInEncyclopedia();
        }

        private void HideVanillaUnitsInEncyclopedia()
        {
            MBObjectManager.Instance.GetObjectTypeList<CharacterObject>().ForEach(x => 
            {
                if (!x.IsTORTemplate() && x.Occupation == Occupation.Soldier)
                {
                    x.HiddenInEncylopedia = true;
                }
            });
        }

        private void OnNewGameCreatedPartialFollowUpEnd(CampaignGameStarter campaignGameStarter, int index)
        {
            if (index == CampaignEvents.OnNewGameCreatedPartialFollowUpEventMaxIndex - 2)
            {
                InitializeHeroes();
                EnsurePartyInfos();
            }
        }

        private void OnHeroCreated(Hero hero, bool arg2)
        {
            if (!_heroInfos.ContainsKey(hero.GetInfoKey()))
            {
                var info = new HeroExtendedInfo(hero.CharacterObject);
                _heroInfos.Add(hero.GetInfoKey(), info);
                if (hero.Template != null) InitializeTemplatedHeroStats(hero);
            }
        }

        private void OnHeroKilled(Hero arg1, Hero arg2, KillCharacterAction.KillCharacterActionDetail arg3, bool arg4)
        {
            if (_heroInfos.ContainsKey(arg1.GetInfoKey()))
            {
                _heroInfos.Remove(arg1.GetInfoKey());
            }
        }

        public static void CreateDefaultInstanceAndLoad()
        {
            _ = new ExtendedInfoManager();
            if (_characterInfos.Count > 0) _characterInfos.Clear();
            TryLoadCharacters(out _characterInfos);
        }

        private static void TryLoadCharacters(out Dictionary<string, CharacterExtendedInfo> infos)
        {
            //construct character info for all CharacterObject templates loaded by the game.
            //this can be safely reconstructed at each session start without the need to save/load.
            Dictionary<string, CharacterExtendedInfo> unitlist = [];
            infos = unitlist;
            try
            {
                var path = TORPaths.TORCoreModuleExtendedDataPath + "tor_extendedunitproperties.xml";
                if (File.Exists(path))
                {
                    var ser = new XmlSerializer(typeof(List<CharacterExtendedInfo>));
                    var list = ser.Deserialize(File.OpenRead(path)) as List<CharacterExtendedInfo>;
                    foreach (var item in list)
                    {
                        if (!infos.ContainsKey(item.CharacterStringId))
                        {
                            infos.Add(item.CharacterStringId, item);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error(e.ToString());
                throw e; //TODO handle this more gracefully.
            }
        }

        private void InitializeHeroes()
        {
            foreach (var hero in Hero.AllAliveHeroes)
            {
                if (!_heroInfos.ContainsKey(hero.GetInfoKey()))
                {
                    var info = new HeroExtendedInfo(hero.CharacterObject);
                    _heroInfos.Add(hero.GetInfoKey(), info);
                }
            }
        }

        private static void InitializeTemplatedHeroStats(Hero hero)
        {
            var template = hero.Template;
            int castingLevel = 0;
            if (template.IsTORTemplate() && template.Occupation == Occupation.Wanderer)
            {
                var info = hero.GetExtendedInfo();
                if (info == null) return;
                foreach (var attribute in template.GetAttributes())
                {
                    hero.AddAttribute(attribute);
                }
                foreach (var ability in template.GetAbilities())
                {
                    hero.AddAbility(ability);
                }
            }
        }

        private void OnPartyCreated(MobileParty party)
        {
            if (!_partyInfos.ContainsKey(party.StringId) && party.IsLordParty) _partyInfos.Add(party.StringId, new MobilePartyExtendedInfo());
        }

        private void OnPartyDestroyed(MobileParty destroyedParty, PartyBase destroyerParty)
        {
            if (_partyInfos.ContainsKey(destroyedParty.StringId)) _partyInfos.Remove(destroyedParty.StringId);
        }

        private void EnsurePartyInfos()
        {
            foreach (var party in MobileParty.AllLordParties)
            {
                OnPartyCreated(party);
            }
        }

        public override void SyncData(IDataStore dataStore)
        {
            dataStore.SyncData("_heroInfos", ref _heroInfos);
            dataStore.SyncData("_partyInfos", ref _partyInfos);
        }
    }
}
