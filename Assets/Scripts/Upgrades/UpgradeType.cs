namespace HazeBuster.Upgrades
{
    /// <summary>
    /// Enumeration of all in-run roguelike upgrade options.
    /// </summary>
    public enum UpgradeType
    {
        // Water Gun Primary Weapon
        WaterGunFireRate,
        WaterGunDamage,
        WaterGunMultishot,
        WaterGunSize,
        WaterGunPierce,

        // Vision & Haze Light
        VisionRadius,

        // Utility & Stats
        PickupRange,       // Expands Morning Dew magnet absorption radius
        PlayerSpeed,       // Increases Scientist movement speed
        MaxHealth,         // Increases Suit Max Integrity

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
