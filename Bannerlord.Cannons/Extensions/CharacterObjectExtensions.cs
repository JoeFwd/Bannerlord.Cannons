using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TOR_Core.Extensions.ExtendedInfoSystem;

namespace TOR_Core.Extensions
{
    public static class CharacterObjectExtensions
    {
        public static bool IsTORTemplate(this CharacterObject characterObject)
        {
            return characterObject.StringId.StartsWith("tor_");
        }

        public static List<string> GetAbilities(this BasicCharacterObject characterObject)
        {
            var list = new List<string>();
            var info = ExtendedInfoManager.GetCharacterInfoFor(characterObject.StringId);
            if (info != null)
            {
                list.AddRange(info.Abilities);
            }
            return list;
        }

        public static List<string> GetAttributes(this BasicCharacterObject characterObject)
        {
            var list = new List<string>();
            var info = ExtendedInfoManager.GetCharacterInfoFor(characterObject.StringId);
            if (info != null)
            {
                list.AddRange(info.CharacterAttributes);
            }
            return list;
        }

        public static bool HasAttribute(this BasicCharacterObject characterObject, string attributeName)
        {
            return characterObject.GetAttributes().Contains(attributeName);
        }
    }
}
