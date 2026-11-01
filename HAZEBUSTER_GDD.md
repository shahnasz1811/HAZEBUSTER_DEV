# HAZEBUSTER — Game Design Document (GDD)

**Genre**: 2D Top-Down Action Roguelite / Survival  
**Visual Style**: 2D Pixel Art with Dynamic URP 2D Lighting (Limited Vision / Haze Fog)  
**Target Engine**: Unity 6 (6000.4.2f1) — Universal Render Pipeline (URP 2D)  
**Target Audience**: Action Roguelite Gamers & Educational Players  
**Core Inspiration**: *"Everything is a Crab"*, *Vampire Survivors*, *Nuclear Throne*

---

## 1. Game Premise & Educational Theme

In tropical peat swamp forests (*Gambut*), illegal slash-and-burn clearing and prolonged droughts ignite subterranean fires. These smoldering fires release toxic smog, blanketing the region in hazardous haze (PM2.5 particles).

You play as an **Environmental Scientist Engineer** deployed into the ground zero of the crisis. Equipped with an experimental **Hydro-Cooling Backpack**, high-pressure water tools, and thermal sensors, your mission is to push back the toxic haze, extinguish smoldering fire hazards, collect purified atmospheric moisture (*Morning Dew*), and confront the roaring **Fire Boss** (the wildfire nexus) before the forest reaches irreversible ecological collapse.

---

## 2. Core Gameplay Loop

Explore Smoldering Peatland (Haze limits vision)
  ↓
Extinguish Fire Hazards (Burned Stumps, Embers, Gambut)
  ↓
Absorb Morning Dew / Moisture (EXP & Water Pressure)
  ↓
Level Up & Choose Upgrades (Weapons, Vision Light, Dash perks)
  ↓
Evolve Build (Sprinklers, Foam Bombs, Hydro-Dash)
  ↓
Survive Escalating Waves (10-15 Min Survival Clock)
  ↓
Defeat the Mega Fire Boss (Extinguish the Wildfire Nexus)
  ↓
Meta-Progression Lab (Spend Eco-Credits on Permanent Perks)

---

## 3. Player Character & Controls

### 3.1 Protagonist: The Scientist Engineer
* **Visual**: Pixel-art scientist in fire-resistant environmental gear, wearing a respirator mask, visor, and a high-tech water tank with gauge lights on their back.
* **Controls**:
  * **Movement**: `W, A, S, D` (8-directional top-down movement).
  * **Aiming**: Mouse Cursor (Water nozzle rotates smoothly toward pointer).
  * **Firing**: `Left Mouse Button` (Hold to continuously fire equipped weapons).
  * **Dash / Evasive Maneuver**: `Spacebar` or `Right Mouse Button` (Quick sprint burst with invulnerability frames).
  * **Pause / Upgrade Menu**: Automatic on Level Up or `Escape`.

### 3.2 Dash Mechanic & Progression
Dashing is a vital survival tool against fast-moving embers and creeping peat fires.
* **Base Dash**: 
  * Cooldown: 1.5 seconds.
  * Dash Distance: 4.5 units over 0.2 seconds.
  * Invulnerability: Immune to fire and contact damage during the dash.
* **In-Run Dash Upgrades**:
  * **Hydro-Dash**: Leaves a trail of pressurized water on the ground behind the player that extinguishes burning ground and damages enemies.
  * **Steam Burst**: Emits an explosive burst of cooling steam at the start and end of the dash, knocking back nearby burning enemies.
  * **Double Tank (Extra Charge)**: Adds a 2nd (and 3rd) dash charge so you can dash in rapid succession.
  * **Turbo Turbine**: Reduces dash cooldown by 20% / 40% / 60%.

---

## 4. The Vision & Haze Mechanic

The defining atmosphere comes from **limited visibility caused by peatland haze**:
1. **Smoky Global Ambience**: The scene has a dark, yellowish-gray URP 2D Global Light representing thick haze and airborne soot.
2. **Dynamic Vision Circle**: The player carries an experimental high-intensity halo lamp (URP 2D Point Light). Only entities and ground within this circle are clearly visible.
3. **Audio-Visual Cues**: Outside your vision circle, burning enemies crackle, hiss, and glow faintly as red embers in the gloom.
4. **Vision Upgrades**:
   * **High-Beam Halogen**: Increases the outer radius of the vision circle.
   * **Thermal Visor**: Highlights burning enemies slightly beyond the standard light circle.
   * **Fog Penetrator**: Reduces edge feathering, giving crisper visibility at long range.

---

## 5. Weapons & In-Run Arsenal

Every run starts with the primary **Water Gun**. As the player absorbs Morning Dew and levels up, they can upgrade existing weapons or equip secondary experimental gear:

| Weapon | Description | Upgrade Paths |
| :--- | :--- | :--- |
| **Water Gun** *(Primary)* | Shoots pressurized water droplets towards the mouse cursor. | **Fire Rate**, **Multi-Shot / Spread** (1 -> 2 -> 3 -> 5 stream), **Droplet Size & Pierce**, **Water Pressure** (Velocity & Knockback). |
| **Peat Sprinkler** *(Secondary)* | Rotating orbital head that continuously sprays a 360-degree radial ring of water droplets. | **Rotation Speed**, **Radius**, **Droplet Density**, **Cooling Mist** (slows enemies caught in the spray). |
| **Peat Extinguisher Foam Bomb** *(Secondary)* | Lobbed chemical-cooling canister that lands and bursts into an expanding foam puddle. | **Explosion Radius**, **Puddle Duration**, **Foam Potency** (quenches burning ground instantly and drenches passing enemies). |
| **Vision Light** *(Passive / Gear)* | High-power beam cutting through the haze. | **Light Radius**, **Intensity**, **Stun Flash** (periodically blinds/stuns nearby embers). |

---

## 6. Hazard & Enemy Bestiary

All enemies represent realistic components of the Southeast Asian / tropical peatland haze crisis:

### 6.1 Small Ember (*Bara Api*)
* **Concept**: Airborne burning embers blown by the wind.
* **Behavior**: Fast, zig-zag swarmers that rush the player in packs. Low health, easily extinguished by a single water shot.
* **Educational Note**: Illustrates how dry winds carry embers miles away, sparking new fires (*spotting*).

### 6.2 Burned Stump (*Tunggul Bakar*)
* **Concept**: Smoldering charred tree stump left from slash-and-burn logging.
* **Behavior**: Slow, heavily armored tank. Periodically expels a dense ring of dark smoke particles that pushes the player's vision light inward.
* **Educational Note**: Stumps retain heat for weeks deep inside their roots even after the surface flame is gone.

### 6.3 Gambut Monster (*Api Bawah Tanah*)
* **Concept**: Subterranean peat fire creeping invisibly under dried moss.
* **Behavior**: Moves semi-concealed underground, surfacing to spit fiery sparks and leaving patches of **Burning Ground** that damage the player if stepped on.
* **Educational Note**: Gambut (peat) is rich in organic carbon and burns underground with minimal oxygen, making it extremely difficult to detect and extinguish.

### 6.4 The Mega Fire Boss: The Wildfire Nexus (*Raja Kebakaran*)
* **Spawn**: Triggered at the climax of the run (e.g. 10:00 or 15:00 mark).
* **Appearance**: Colossal flaming tree-beast entwined with burning peat roots and smoke dragons.
* **Attack Phases**:
  1. **Phase 1 (Blaze Surge)**: Emits concentric rings of fireballs and sweeps the arena with a blazing fire breath beam.
  2. **Phase 2 (Haze Smother)**: Dims the player's vision radius by 50% with an ultra-dense smoke wave while summoning Ember swarms.
  3. **Phase 3 (Gambut Inferno)**: Turns large sectors of the ground into flaming peat hotspots, requiring active dashing and foam bombs to clear safe zones.

---

## 7. Meta-Progression: The Peatland Rehabilitation Lab

Between runs, the player returns to the **Eco-Research Base**:
* **Currency**: **Eco-Credits / Restored Peat Seeds** collected from runs.
* **Permanent Upgrades**:
  1. **Reinforced Fire Suit**: Increases base Max HP (100 -> 120 -> 140...).
  2. **Hydraulic Footwear**: Increases base Movement Speed (+5% per rank).
  3. **High-Flow Regulator**: Increases starting Water Gun fire rate (+8% per rank).
  4. **Hydro-Turbine**: Decreases base Dash Cooldown (-10% per rank).
  5. **Atmospheric Condenser**: Increases Dew Magnet Radius (droplets fly to the player from further away).
  6. **Auxiliary Battery**: Increases base Vision Light radius (+15% per rank).
