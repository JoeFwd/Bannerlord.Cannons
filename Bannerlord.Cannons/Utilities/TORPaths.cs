using TaleWorlds.ModuleManager;

namespace TOR_Core.Utilities
{
    public static class TORPaths
    {
        /// <summary>
        /// The root directory of the TOR Core module
        /// </summary>
        public static string TORCoreModuleRootPath
        {
            get { return ModuleHelper.GetModuleFullPath("Bannerlord.Cannons"); }
        }

        /// <summary>
        /// The ModuleData/tor_custom_xmls directory of the TOR Core module
        /// </summary>
        public static string TORCoreModuleExtendedDataPath
        {
            get { return TORCoreModuleDataPath + "tor_custom_xmls/"; }
        }

        /// <summary>
        /// The ModuleData directory of the TOR Core module
        /// </summary>
        public static string TORCoreModuleDataPath
        {
            get { return TORCoreModuleRootPath + "ModuleData/"; }
        }
    }
}
