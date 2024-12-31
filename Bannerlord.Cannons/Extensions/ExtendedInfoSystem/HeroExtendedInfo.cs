using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.SaveSystem;

namespace TOR_Core.Extensions.ExtendedInfoSystem
{
    public class HeroExtendedInfo(CharacterObject character)
    {
        [SaveableField(0)] public List<string> AcquiredAbilities = [];
        [SaveableField(1)] public List<string> AcquiredAttributes = [];
        [SaveableField(2)] private CharacterObject _baseCharacter = character;
        [SaveableField(3)] private List<string> _selectedAbilities = [];

        public CharacterObject BaseCharacter => _baseCharacter;

        public List<string> AllAbilities
        {
            get
            {
                var list = new List<string>();
                if (_baseCharacter != null)
                {
                    list.AddRange(_baseCharacter.GetAbilities());
                    if (list.Count <= 0 && _baseCharacter.OriginalCharacter != null && _baseCharacter.OriginalCharacter.IsTemplate)
                    {
                        list.AddRange(_baseCharacter.OriginalCharacter.GetAbilities());
                    }
                }
                list.AddRange(AcquiredAbilities);
                
                return list;
            }
        }

        public List<string> SelectedAbilities
        {
            get
            {
                if (_selectedAbilities.Count > 0) return _selectedAbilities;
                else return AllAbilities;
            }
        }

        public List<string> AllAttributes
        {
            get
            {
                var list = new List<string>();
                if (_baseCharacter != null)
                {
                    list.AddRange(_baseCharacter.GetAttributes());
                    if (list.Count <= 0 && _baseCharacter.OriginalCharacter != null && _baseCharacter.OriginalCharacter.IsTemplate)
                    {
                        list.AddRange(_baseCharacter.OriginalCharacter.GetAttributes());
                    }
                }
                list.AddRange(AcquiredAttributes);
                return list;
            }
        }
    }
}
