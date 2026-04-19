using System;
using System.Linq;
using System.Reflection;
using TaleWorlds.MountAndBlade;

namespace Bannerlord.Cannons.Initialisation
{
    public class MissionLogicRegistrar
    {
        public void AddTo(Mission mission)
        {
            var missionLogics = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(type =>
                    typeof(MissionLogic).IsAssignableFrom(type) &&
                    !type.IsAbstract &&
                    type.GetConstructor(Type.EmptyTypes) != null)
                .Select(type => Activator.CreateInstance(type) as MissionLogic)
                .Where(instance => instance != null);

            foreach (var missionLogic in missionLogics)
                mission.AddMissionBehavior(missionLogic!);
        }
    }
}
