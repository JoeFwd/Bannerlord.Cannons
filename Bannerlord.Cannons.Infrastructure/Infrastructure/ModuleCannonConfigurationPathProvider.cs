using System.Collections.Generic;
using System.IO;
using TaleWorlds.ModuleManager;

namespace Bannerlord.Cannons.Infrastructure
{
    public class ModuleCannonConfigurationPathProvider : ICannonConfigurationPathProvider
    {
        private const string CannonConfigurationRelativePath = "ModuleData/CustomXml/cannons.xml";

        public IEnumerable<string> GetConfigurationPaths()
        {
            foreach (var module in ModuleHelper.GetModules())
            {
                yield return Path.Combine(
                    ModuleHelper.GetModuleFullPath(module.Id),
                    CannonConfigurationRelativePath);
            }
        }
    }
}
