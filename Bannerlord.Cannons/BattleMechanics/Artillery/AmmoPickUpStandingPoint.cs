﻿using Bannerlord.Cannons;
using TaleWorlds.MountAndBlade;

namespace TOR_Core.BattleMechanics.Artillery;

public class AmmoPickUpStandingPoint : StandingPointWithWeaponRequirement
{
    private readonly IArtilleryCrewProvider _artilleryCrewProvider = ArtilleryCrewProviderFactory.CreateArtilleryCrewProvider();
    
    public override bool IsDisabledForAgent(Agent agent)
    {
        return !_artilleryCrewProvider.IsArtilleryCrew(agent) || base.IsDisabledForAgent(agent);
    }
}