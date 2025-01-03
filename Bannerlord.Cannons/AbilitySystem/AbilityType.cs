namespace TOR_Core.AbilitySystem
{
    public enum AbilityType
    {
        ItemBound
    }

    public enum AbilityEffectType
    {
        ArtilleryPlacement,
    }

    public enum AbilityTargetType
    {
        Self,
        SingleEnemy,
        SingleAlly,
        EnemiesInAOE,
        AlliesInAOE,
        WorldPosition,
        GroundAtPosition
    }

    public enum CastType
    {
        Instant,
    }

    public enum TriggerType
    {
        TickOnce,
    }

    //This is for triggeredeffects.
    public enum TargetType
    {
        Friendly,
        Enemy,
        All,
        FriendlyHero,
        EnemyHero,
        Self
    }
}