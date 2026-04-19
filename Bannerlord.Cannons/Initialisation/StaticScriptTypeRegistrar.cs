using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.DotNet;
using TaleWorlds.Engine;

namespace Bannerlord.Cannons.Initialisation
{
    public class StaticScriptTypeRegistrar
    {
        public void RegisterAllScriptComponentBehaviors()
        {
            var types = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(type =>
                    typeof(ScriptComponentBehavior).IsAssignableFrom(type) &&
                    !type.IsAbstract &&
                    !type.IsInterface)
                .ToDictionary(type => type.Name, type => type);

            Managed.AddTypes(types);
        }
    }
}
