namespace HazeBuster.Upgrades
{
    /// <summary>
    /// Enumeration of all in-run roguelike upgrade options.
    /// </summary>
    public enum UpgradeType
    {
        // Water Gun
        WaterGunFireRate,
        WaterGunDamage,
        WaterGunMultishot,
        WaterGunSize,
        WaterGunPierce,

        // Vision & Haze Light
        VisionRadius,

        // Secondary Weapon: Peat Sprinkler
        UnlockSprinkler,
        SprinklerFireRate,
        SprinklerDamage,
        SprinklerNozzles,

        // Secondary Weapon: Foam Bomb Mortar
        UnlockFoamBomb,
        FoamBombFireRate,
        FoamBombDamage,
        FoamBombRadius,

        // Dash Abilities & Evolutions
        DashHydroTrail,
        DashSteamBurst,
        DashExtraCharge,
        DashCooldown,

        // Emergency Maintenance
        SuitHeal
    }
}
