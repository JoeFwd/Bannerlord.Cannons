using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using TOR_Core.AbilitySystem.Crosshairs;
using TOR_Core.Utilities;

namespace TOR_Core.AbilitySystem
{
    public class AbilityFactory
    {
        private static Dictionary<string, AbilityTemplate> _templates = new Dictionary<string, AbilityTemplate>();
        private static string _filename = "tor_abilitytemplates.xml";

        public static void LoadTemplates()
        {
            var ser = new XmlSerializer(typeof(List<AbilityTemplate>), new XmlRootAttribute("AbilityTemplates"));
            var path = TORPaths.TORCoreModuleExtendedDataPath + _filename;
            if (File.Exists(path))
            {
                var list = ser.Deserialize(File.OpenRead(path)) as List<AbilityTemplate>;
                foreach (var item in list)
                {
                    _templates.Add(item.StringID, item);
                }
            }
        }

        public static Ability CreateNew(string id)
        {
            Ability ability = null;
            if (_templates.ContainsKey(id))
            {
                ability = InitializeAbility(_templates[id]);
            }
            return ability;
        }

        private static Ability InitializeAbility(AbilityTemplate template)
        {
            Ability ability = null;
 
            if(template.AbilityType == AbilityType.ItemBound)
            {
                ability = new ItemBoundAbility(template);
            }
            return ability;
        }

        public static AbilityCrosshair InitializeCrosshair(AbilityTemplate template)
        {
            return new Pointer();
        }
    }
}
