# HAZEBUSTER — Unity 6 Setup & Quickstart Guide
*A Step-by-Step Guide for Art & Design Students (Zero Coding Experience Required!)*

Welcome to **HAZEBUSTER**! Every system requested in your brief has been fully written in clean C# scripts inside your project folder. You do not need to write any code.

This guide will walk you through launching the game in **Unity 6000.4.2f1**, generating your pixel art placeholders in 1 click, and setting up the haze lighting and player systems.

---

## 1. Creating the Unity Project

1. Open **Unity Hub**.
2. Click the blue **New project** button in the top right.
3. Select your installed Unity Editor version: **`6000.4.2f1`**.
4. In the template list, select **2D (URP)** *(Universal Render Pipeline)*.
   > Make sure you choose **2D (URP)**! The dynamic limited vision circle (cutting through thick peatland haze) relies on Unity's built-in 2D Lighting system.
5. Set the **Project Name** to `HAZEBUSTER`.
6. Set the **Location** to `d:\AI_prototype_project\` (or choose this existing project folder).
7. Click **Create project** and let Unity initialize.

---

## 2. Generating Pixel Art Sprites (1-Click Tool)

We built an automatic pixel art generator specifically so you can play immediately before drawing all your final sprites!

1. In the top Unity menu bar, look for the new menu item:  
   **`HazeBuster` -> `Generate Pixel Art Placeholders`**
2. Click it!
3. Open your Unity **Project Window** at the bottom: navigate to **`Assets/Sprites`**.
4. You will see 10 retro pixel-art sprites ready to go:
   * **`ScientistPlayer.png`** (Hazmat engineer with respirator & backpack water tank)
   * **`WaterDroplet.png`** (Pressurized water projectile)
   * **`DewPickup.png`** (Glowing morning dew / EXP crystal)
   * **`EmberEnemy.png`** (Glowing wildfire ember swarmer)
   * **`BurnedStumpEnemy.png`** (Charred tree stump with smoldering cracks)
   * **`GambutEnemy.png`** (Subterranean peat monster)
   * **`FireBoss.png`** (Colossal wildfire nexus boss)
   * **`BurningGround.png`** (Smoldering peat hazard tile)
   * **`FoamBomb.png`** (Chemical extinguishing canister)
   * **`FoamPuddle.png`** (Expanding cooling foam puddle)

> When you create your own sprites in **Aseprite**, **Photoshop**, or **Krita**, simply save your `.png` files into this folder and assign them to the Sprite Renderers. Set their **Filter Mode** to `Point (no filter)` and **Compression** to `None` for crisp pixel art!

---

## 3. Creating the Haze Atmosphere (2D Lighting)

To give the game the limited vision feel inspired by *"Everything is a Crab"*:

### Step A: The Murky Haze Global Light
1. In the **Hierarchy** window (left), Right-click -> **2D Object** -> **Light 2D** -> **Global Light 2D**.
2. Name it `HazeGlobalLight`.
3. In the **Inspector** (right):
   * Set **Color** to a dark, smoky yellowish-gray (e.g. Hex `#3B352E` or RGB `59, 53, 46`).
   * Set **Intensity** to `0.2` or `0.3`.  
   *(Notice your Scene goes dark and smoggy — everything outside your light will be shrouded in gloom!)*

### Step B: The Player's Dynamic Vision Circle
1. In the **Hierarchy**, create an empty GameObject or Sprite for the player (see Section 4).
2. Right-click on your **Player** -> **2D Object** -> **Light 2D** -> **Point Light 2D**.
3. Name it `VisionLight`.
4. In the **Inspector**:
   * Set **Color** to warm soft white / lantern glow (e.g. `#FFF7E6`).
   * Set **Inner Radius** to `3.0`.
   * Set **Outer Radius** to `6.0`.
   * Set **Intensity** to `1.0`.
5. Now, wherever the player walks, a clean circle of light cuts through the haze!

---

## 4. Setting Up the Player Character

1. In the **Hierarchy**, Right-click -> **Create Empty** -> Rename to `Player`.
2. Set its **Tag** to `Player` (at the top of the Inspector).
3. Add the following components in the Inspector (click **Add Component**):
   * **`Sprite Renderer`**: Drag `ScientistPlayer.png` into the **Sprite** field. Set **Sorting Layer** to `Foreground` or Order in Layer `5`.
   * **`Rigidbody 2D`**: Set **Body Type** to `Dynamic`, **Gravity Scale** to `0`, and check **Constraints -> Freeze Rotation Z**.
   * **`Circle Collider 2D`**: Radius approx `0.4`.
   * **`Player Controller`** (`PlayerController.cs`): Handles WASD movement, mouse aim, and Dash.
   * **`Player Health`** (`PlayerHealth.cs`): Handles HP, suit damage invulnerability, and game over.
   * **`Player Experience`** (`PlayerExperience.cs`): Handles absorbing dew, leveling up, and magnet radius.
   * **`Player Vision`** (`PlayerVision.cs`): Drag your `VisionLight` into the **Player Light** slot.
4. **Attach Weapons**:
   * Inside the Player, Right-click -> **Create Empty** -> Rename to `WeaponMount`.
   * Drag `WeaponMount` into the **Weapon Mount** field of `PlayerController`.
   * Add **`Water Gun`** (`WaterGun.cs`) to `WeaponMount`.
   * Add **`Peat Sprinkler`** (`PeatSprinkler.cs`) to `Player` (starts locked until you choose it on level-up!).
   * Add **`Foam Bomb Launcher`** (`FoamBombLauncher.cs`) to `Player` (starts locked until chosen!).

---

## 5. Setting Up Prefabs

For objects that spawn continuously (projectiles, dew drops, enemies), make **Prefabs** by dragging them from the Hierarchy into an `Assets/Prefabs` folder:

1. **Water Projectile**:
   * GameObject with `SpriteRenderer` (`WaterDroplet.png`), `Rigidbody2D` (Kinematic / Dynamic with Gravity 0), `CircleCollider2D` (Is Trigger = True), and `WaterProjectile.cs`.
   * Drag into `Assets/Prefabs/WaterProjectile.prefab`.
   * Assign this prefab to the **Projectile Prefab** field on the Player's `WaterGun`.

2. **Dew Pickup**:
   * GameObject with `SpriteRenderer` (`DewPickup.png`), `CircleCollider2D` (Is Trigger = True), and `DewPickup.cs`.
   * Drag into `Assets/Prefabs/DewPickup.prefab`.

3. **Burning Ground**:
   * GameObject with `SpriteRenderer` (`BurningGround.png`), `CircleCollider2D` (Is Trigger = True), and `BurningGround.cs`.
   * Assign the `DewPickup` prefab to its **Dew Prefab** field.
   * Tag: `Hazard`. Drag into `Assets/Prefabs/BurningGround.prefab`.

4. **Enemies (Ember, Burned Stump, Gambut)**:
   * Each enemy has: `SpriteRenderer` (respective sprite), `Rigidbody2D` (Gravity 0, Freeze Z), `Collider2D` (Is Trigger = True or standard), and its script (`EmberEnemy.cs`, `BurnedStumpEnemy.cs`, `GambutEnemy.cs`).
   * Tag all of them: **`Enemy`**.
   * Assign `DewPickup` prefab to the **Dew Prefab** slot of each enemy!

5. **Fire Boss**:
   * GameObject with `SpriteRenderer` (`FireBoss.png`), `CircleCollider2D`, `Rigidbody2D`, and `FireBoss.cs`.
   * Tag: **`Enemy`**.

---

## 6. The Spawner & Core Managers

In your Scene Hierarchy:
1. Create an empty GameObject named `_GameManagers`.
2. Add:
   * **`Game Manager`** (`GameManager.cs`)
   * **`Sound Manager`** (`SoundManager.cs`) *(Has built-in retro sounds — works without any audio files!)*
   * **`Upgrade Manager`** (`UpgradeManager.cs`)
   * **`Meta Progression Manager`** (`MetaProgressionManager.cs`)
   * **`Enemy Spawner`** (`EnemySpawner.cs`)
3. On the **`Enemy Spawner`**, drag your enemy prefabs into their respective slots (`Ember Prefab`, `Burned Stump Prefab`, `Gambut Prefab`, `Fire Boss Prefab`).

---

## 7. Controls Summary

| Action | Control | Notes |
| :--- | :--- | :--- |
| **Move** | `W, A, S, D` or Arrow Keys | 8-directional top-down movement |
| **Aim** | Mouse Pointer | Water nozzle tracks your cursor |
| **Shoot Water** | `Left Mouse Button` (Hold) | Fires pressurized water droplets |
| **Dash** | `Spacebar` or `Right Mouse Button` | Quick burst with invulnerability frames |
| **Pause Game** | `Escape` | Toggles pause |
