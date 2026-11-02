# HAZEBUSTER

A 2D top-down **action roguelite** about pushing back peatland haze. You play an Environmental Scientist Engineer with a Hydro-Cooling Backpack, putting out smouldering peat (*Gambut*) fires before the forest collapses.

| | |
| :--- | :--- |
| Genre | 2D top-down action roguelite / survival |
| Engine | Unity 6000.4.2f1 |
| Render pipeline | Built-in (2D template). The URP package is **not** installed |
| Input | Input System 1.19.0 + legacy Input Manager (Active Input Handling = Both) |
| UI | uGUI with legacy `Text` |
| Code | C#, namespaces `HazeBuster.*` |
| Inspiration | *Everything is a Crab*, *Vampire Survivors*, *Nuclear Throne* |
| Design doc | [`HAZEBUSTER_GDD.md`](HAZEBUSTER_GDD.md) |

> **Status (2 Nov 2026): early prototype.** Most gameplay scripts exist, but the saved scene (`SampleScene`) contains only the Main Camera, several prefabs are missing, and parts of the GDD are not built yet. A round of bug fixes and arena work was written on 2 Nov 2026 **but has not been compiled in Unity yet**: open the project, let it compile, and fix any console errors before building further. See [Known issues](#known-issues), [GDD coverage](#gdd-coverage), [Roadmap](#roadmap) and the [Progress log](#progress-log).

---

## Premise

Illegal slash-and-burn clearing and drought ignite subterranean fires in tropical peat swamp forests. The fires release toxic haze (PM2.5). You push the haze back, extinguish fire hazards, collect purified *Morning Dew*, and face the **Wildfire Nexus** boss before the ecosystem collapses.

## Core loop

```
Explore smoldering peatland (haze limits vision)
  -> extinguish fire hazards
  -> absorb Morning Dew (EXP)
  -> level up, pick 1 of 3 upgrades
  -> evolve your build (sprinklers, foam bombs, hydro-dash)
  -> survive escalating waves
  -> defeat the Wildfire Nexus boss
  -> spend Eco-Credits on permanent perks in the Peatland Rehabilitation Lab
```

## Controls

| Action | Input |
| :--- | :--- |
| Move | `W` `A` `S` `D` (or arrow keys) |
| Aim | Mouse cursor |
| Fire water | Hold left mouse button |
| Dash (invulnerable) | `Space`, `Left Shift` or right mouse button |
| Pause | `Esc` (level-up opens its own menu automatically) |

---

## Quick start (project as it stands today)

1. Open the folder in Unity Hub with editor **6000.4.2f1**. Let it compile and read the Console. The code has not been compiled since the 2 Nov 2026 changes.
2. The tags `Enemy` and `Hazard` are added automatically by `TagSetup` when Unity loads the scripts. If they are missing, run **HazeBuster > Ensure Required Tags**.
3. Run **HazeBuster > Generate Pixel Art Placeholders** to create the sprites in `Assets/Sprites`.
4. Build the player, prefabs and managers following [`UNITY_SETUP_GUIDE.md`](UNITY_SETUP_GUIDE.md) sections 4-6. Skip the 2D-light steps: they need URP, which this project does not have. Put the Player on the `Player` tag.
5. Run **HazeBuster > Create Complete Game UI & Level-Up Menu**.
6. Optional: **HazeBuster > Build Arena Walls (60x60)**. The player is already kept inside the arena by code, so the walls are only extra.
7. Press Play.

| Menu item | Tool | What it does |
| :--- | :--- | :--- |
| HazeBuster > Ensure Required Tags | `TagSetup` | Adds the `Enemy` and `Hazard` tags if they are missing. Also runs by itself on script reload |
| HazeBuster > Generate Pixel Art Placeholders | `PixelArtGenerator` | Writes 11 placeholder PNGs and sets Point filter / no compression |
| HazeBuster > Create Complete Game UI & Level-Up Menu | `UISetupTool` | Builds the HUD and the 3-card level-up modal |
| HazeBuster > Build Arena Walls (60x60) | `ArenaSetupTool` | Adds four invisible box-collider walls, sized from `ArenaBounds.halfSize` |

---

## Project structure

```
Assets/
  Editor/             ArenaSetupTool, PixelArtGenerator, TagSetup, UISetupTool
  Prefabs/
    Enemies/          Ember, BurnedStump, Gambut
    Weapons/          WaterProjectile
    Resources/        DewPickup
  Scenes/             SampleScene (Main Camera only)
  Scripts/
    Boss/             FireBoss, FireProjectile
    Core/             GameManager, SoundManager
    Enemies/          EnemyBase, EmberEnemy, BurnedStumpEnemy, GambutEnemy, EnemySpawner
    Environment/      BurningGround, DewPickup
    MetaProgression/  MetaProgressionManager
    Player/           PlayerController, PlayerHealth, PlayerExperience, PlayerVision, WaterTrail
    UI/               UIManager, UpgradeCardUI
    Upgrades/         UpgradeType, UpgradeData, UpgradeManager
    Utility/          ArenaBounds, CameraFollow, InfiniteGround, InputHelper, PlayerLocator, PoolManager
    Weapons/          WeaponBase, WaterGun, WaterProjectile, PeatSprinkler,
                      FoamBombLauncher, FoamBombProjectile, FoamPuddle
  Sprites/            11 generated placeholder PNGs
```

## How the systems work today

| System | Current behaviour |
| :--- | :--- |
| Run flow | `GameManager` states: Playing, LevelingUp, Paused, GameOver, Victory. `Time.timeScale` is 0 outside Playing. Boss triggers at `runDurationToBoss` (default 300 s). Retry reloads the scene. Every manager belongs to its scene, so a Retry starts completely fresh. Credits are paid out once per run. |
| Player | 100 HP, move speed 5, 1 s hurt invulnerability (separate from dash invulnerability). Dash: 1.5 s cooldown, speed 16 for 0.2 s, 1 charge. Movement uses `Rigidbody2D`. Kept inside `ArenaBounds` (60 x 60) by code. |
| Experience | Dew tiers: small 1, medium 5, large 20 EXP. First level needs 6 EXP, then `round(previous x 1.3) + 3`. Pickup radius starts at 1.2. |
| Water Gun | 15 damage, 14 units/s, 2 shots/s, 12 degree fan spread, knockback 6. Droplets are pooled and reused. |
| Peat Sprinkler | Locked until chosen. 4 nozzles, 90 degrees/s, 8 damage. |
| Foam Bomb Mortar | Locked until chosen. 0.4 shots/s, impact 35, radius 3.5, puddle lasts 5 s. |
| Enemies | Ember 15 HP / speed 4.2. Burned Stump 80 HP / 1.4 (drops 20 EXP). Gambut 45 HP / 2.0 (drops 5 EXP, leaves burning ground). Health is multiplied by `1 + 0.12 x minutes` at spawn. Enemies more than 35 units from the player are removed (the boss is not). |
| Boss | Wildfire Nexus, 1200 HP. Phases switch at 66 % and 33 % HP. Attacks: fireball nova, fireball barrage, ember summon, burning-ground patches. It spawns on the roomier side of the arena. |
| Spawner | Interval goes 1.8 s to 0.35 s over the run. Under 30 % of the run: embers only. 30-65 %: embers and stumps. Over 65 %: all three. Up to 65 alive. Spawn points stay inside the arena. An unassigned prefab falls back to an ember. |
| Upgrades | 19 built-in cards, 3 random per level-up, game pauses while choosing. If one pickup gives several levels, each level-up shows its own cards in turn. |
| Meta-progression | 6 permanent perks, 5 ranks each, costs 20 / 50 / 100 / 180 / 300, saved in `PlayerPrefs`. Credits = dew collected (+100 on victory). |
| UI | HUD (HP, EXP, level, timer, dash, dew) and level-up modal. `UIManager` has fields for the boss bar, game over, victory and shop panels, but no tool builds them yet. |
| Audio | Procedural 8-bit sound effects, generated once and cached. No audio files needed. |

---

## Known issues

Numbers come from the 1 Nov 2026 audit and are kept stable so other notes can refer to them.

### Fixed on 2 Nov 2026 (written, not yet compiled or play-tested)

| # | What changed |
| :-: | :--- |
| 1 | Enemy health scaling works. The spawner now sets `EnemyBase.healthMultiplier`, and each enemy applies it at the end of its own `Start()` through `FinalizeHealth()`. |
| 3 | `Assets/Editor/TagSetup.cs` adds the `Enemy` and `Hazard` tags automatically. |
| 4 | `DontDestroyOnLoad` is gone from `MetaProgressionManager`. `GameManager`, `SoundManager`, `MetaProgressionManager`, `UpgradeManager` and `UIManager` are scene-scoped singletons: the newest one wins, and each clears its static `Instance` when destroyed. `UIManager` unsubscribes from events. Victory and game over can pay credits only once. |
| 5 | The move-speed and dash-cooldown upgrades call `PlayerController.ApplyMetaBonuses()` instead of resetting to base values. A line in `PlayerController.Start()` that wiped the Lab dash-cooldown bonus right after it was applied was also removed (not in the original list). |
| 6 | Level-ups are queued. Each one shows its own 3 cards, and play resumes after the last. If no upgrade is left to offer, play resumes instead of showing an empty menu. |
| 7 | Dash and hurt invulnerability are separate flags. `PlayerHealth.isInvulnerable` is now read-only, and the dash uses `SetDashInvulnerable(bool)`. |
| 9 | `SoundManager` builds each procedural clip once, reuses it, and destroys the clips with the manager. |

Also found and fixed while working (not in the original list):

- A dew pickup could be counted twice in one frame (double EXP). It now has a `collected` guard.
- Two shots landing in the same frame could kill one enemy twice (double dew, and a double boss victory). `EnemyBase` now has an `isDead` guard, and a dead enemy no longer deals contact damage.
- The boss was removed when the player got more than 35 units away, so a run could not be won. The boss now sets `despawnWhenFar = false`.
- The spawner silently skipped a wave when a prefab was not assigned. It now falls back to an ember, and warns in the Console if the boss prefab is missing.
- Part of #13: `PlayerLocator` finds the player once, so enemies, pickups and the spawner no longer call `FindGameObjectWithTag` each. `PoolManager` reuses water droplets (Water Gun and Peat Sprinkler) and boss fireballs instead of creating and destroying them.

### Still open

| # | Severity | Where | Problem |
| :-: | :--- | :--- | :--- |
| 2 | High | Scene and prefabs | The saved scene has only the Main Camera. There are no Player, FireBoss, FireProjectile, BurningGround, FoamBomb or FoamPuddle prefabs. Without a boss prefab the boss never spawns, so a run cannot be won. |
| 8 | Medium | Sprite import | Sprites import at 100 pixels per unit. A 12 px ember is 0.12 units wide but its collider radius is 0.5. The scene camera size is 5. Characters appear tiny. |
| 10 | Medium | `WaterTrail`, `FoamPuddle` | The Hydro-Dash puddle has no sprite, so it is invisible. `FoamPuddle.slowMultiplier` is never applied. |
| 11 | Low | `FoamBombLauncher` | `Vector3.zero` means "no target", so a target at the origin is ignored. Physics queries run every frame and allocate. |
| 12 | Low | Various | Dash covers 3.2 units (GDD: 4.5). Three Turbo Turbine ranks stack to about 20 / 36 / 49 % (GDD: 20 / 40 / 60 %). Default run is 300 s (GDD: 10-15 min). `Esc` pauses but there is no pause menu. |
| 13 | Low | Performance | Water droplets and boss fireballs are pooled now, but enemies and dew are still instantiated and destroyed. Enemies still use `OnTriggerStay2D`, which fires a lot in swarms. |

## GDD coverage

| GDD section | Status | Notes |
| :--- | :--- | :--- |
| 3.1 Controls | Done | `Esc` pauses, but no pause menu is shown. |
| 3.2 Dash base and invulnerability | Partial | Invulnerability now works (item 7 fixed). Distance is 3.2 units, not 4.5 (item 12). |
| 3.2 Hydro-Dash, Steam Burst, Double Tank, Turbo Turbine | Partial | Steam Burst and extra charges work. Trail is invisible. Turbine stacking differs. |
| 4 Smoky ambience and vision circle | Missing | Needs URP 2D lights. `PlayerVision` only drives a `Light2D` if one exists, and this project has none. |
| 4 Glow and crackle outside the vision circle | Missing | |
| 4 High-Beam Halogen | Partial | The radius value is upgraded, but nothing visible changes without a Light2D. |
| 4 Thermal Visor, Fog Penetrator | Missing | |
| 5 Water Gun: fire rate, multi-shot, size, pierce | Done | Multi-shot adds one stream per rank rather than the 1, 2, 3, 5 table. |
| 5 Water Gun: Water Pressure | Missing | Velocity and knockback are not upgradable. |
| 5 Peat Sprinkler | Partial | Unlock and nozzle count work. Rotation speed, radius and Cooling Mist are missing. |
| 5 Foam Bomb | Partial | Unlock and radius work. Puddle duration and Foam Potency are missing. |
| 5 Vision Light | Partial | Radius only. Intensity and Stun Flash are missing. |
| 6.1 Small Ember | Partial | Zig-zag movement works. Pack spawning does not. |
| 6.2 Burned Stump | Partial | The smoke puff does not push the vision light inward. |
| 6.3 Gambut Monster | Partial | Drops burning ground. No underground, surfacing or spark-spitting behaviour. |
| 6.4 Wildfire Nexus | Partial | Three HP-based phases. No fire-breath beam, Phase 2 does not dim vision, Phase 3 is not sector-based. No prefab. |
| 7 Meta-progression | Partial | Six perks and saving work. No Lab screen. Per-rank values differ from the GDD. |

## Roadmap

A full overhaul was started on 1 Nov 2026. The audit is finished and the Foundation step is written. **Nothing has been compiled in Unity yet.**

- [x] Audit the project against the GDD
- [x] Foundation (written, not yet compiled)
  - [x] Shared arena bounds: `ArenaBounds` is 60 x 60 and is used by the player clamp, the spawner and the wall tool. Added later on 2 Nov 2026 (not compiled): water droplets and boss fireballs end at the arena edge, the camera keeps its view inside the arena, and the ground sits fixed on the arena
  - [x] Cached sound effects
  - [x] Run and save lifecycle fixes
  - [x] Object pooling for the high-volume projectiles: water droplets (Water Gun and Peat Sprinkler) and boss fireballs use `PoolManager`. Enemies and dew are still created and destroyed. They appear only a few per second, so extra reset code is not worth it until profiling shows a need
- [ ] Player and weapons: one stat calculation for meta plus run bonuses, dash charges, every GDD weapon, vision and dash upgrade path
- [ ] Enemies: GDD behaviours, data-driven wave timeline, difficulty scaling, three-phase boss
- [ ] Upgrades: ranks, weighted rolls, evolutions, reroll, polished 3-card menu, pause screen showing the current build
- [ ] Meta-progression: Eco-Research Lab screen, JSON save, GDD perk values
- [ ] Haze: a vision overlay that works without URP, with glow for enemies outside the circle
- [ ] Environment: streamed decor, drifting ash, caps on hazards and dew
- [ ] One-click "Build Complete Game" editor tool (tags, prefabs, scene, UI)
- [ ] Final checks and updated setup guide

### Next step

1. Open the project in Unity, let it compile, and fix any Console errors. `UISetupTool` and `PixelArtGenerator` were not opened this session. If one of them reports an error, look for a use of the removed `PlayerHealth.isInvulnerable` setter.
2. Play-test the pooled projectiles once a scene exists: droplets and fireballs should fire, hit, pierce and expire as before. A reused projectile keeps nothing from its last flight because `Initialize()` resets it. If a droplet or fireball prefab later gets a trail or particle child, clear it when the object is reused. Pooling enemies would need their stat setup and `FinalizeHealth()` moved out of `Start()`, which does not run again on reuse.
3. Arena use is finished (2 Nov 2026, not compiled): projectiles end at the arena edge, `CameraFollow.clampToArena` keeps the view inside the arena, and `InfiniteGround.fitToArena` fixes the ground on the arena. Play-test the camera once a scene exists. At camera size 5 on a 16:9 screen the view is about 17.8 x 10 units, so the camera stops moving when the player is that far from the arena edge.
4. Consider doing the one-click "Build Complete Game" tool (currently late in the roadmap) next. Without a scene, Player and prefabs, none of these fixes can be play-tested (open issue 2).

### Progress log

| Date | Step | State |
| :--- | :--- | :--- |
| 1 Nov 2026 | Audit against the GDD. Added `ArenaBounds.cs` | Done |
| 2 Nov 2026 | Foundation (bug fixes, arena clamping, projectile pooling). New: `Editor/TagSetup.cs`, `Utility/PlayerLocator.cs`, `Utility/PoolManager.cs`. Rewritten: `Core/SoundManager.cs`, `Enemies/EnemyBase.cs`, `Enemies/EnemySpawner.cs`, `Player/PlayerHealth.cs`, `Weapons/WaterProjectile.cs`, `Boss/FireProjectile.cs`. Edited: `Core/GameManager.cs`, `MetaProgression/MetaProgressionManager.cs`, `Upgrades/UpgradeManager.cs`, `UI/UIManager.cs`, `Player/PlayerController.cs`, `Environment/DewPickup.cs`, `Enemies/EmberEnemy.cs`, `Enemies/BurnedStumpEnemy.cs`, `Enemies/GambutEnemy.cs`, `Boss/FireBoss.cs`, `Weapons/WaterGun.cs`, `Weapons/PeatSprinkler.cs`, `Utility/ArenaBounds.cs`, `Editor/ArenaSetupTool.cs` | Written, **not compiled** |
| 2 Nov 2026 | Arena use finished. Edited: `Weapons/WaterProjectile.cs` and `Boss/FireProjectile.cs` (shots end at the arena edge, 2 unit grace), `Utility/CameraFollow.cs` (new `clampToArena`, on by default), `Utility/InfiniteGround.cs` (new `fitToArena`, on by default) | Written, **not compiled** |

Planned approach for the rest: distance-based hit detection instead of physics callbacks, pooled projectiles, enemies and pickups, and one place that combines base, meta and run stat bonuses.

## Documents

| File | Purpose |
| :--- | :--- |
| [`HAZEBUSTER_GDD.md`](HAZEBUSTER_GDD.md) | Game design document (source of truth) |
| [`UNITY_SETUP_GUIDE.md`](UNITY_SETUP_GUIDE.md) | Manual scene, prefab and manager setup. Assumes URP, which is not installed |
| [`NEXT_STEPS_UPGRADE_MENU.txt`](NEXT_STEPS_UPGRADE_MENU.txt) | Level-up menu and tuning notes. Its boss timeline (5:00) differs from the GDD (10:00 or 15:00) |
