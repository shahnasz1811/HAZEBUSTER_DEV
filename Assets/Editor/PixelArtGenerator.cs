using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace HazeBuster.Editor
{
    /// <summary>
    /// Unity Editor Utility created specifically for Art & Design students!
    /// Generates clean, colorful 16x16 and 32x32 retro pixel-art placeholder sprites
    /// directly into your Assets/Sprites folder.
    /// Access via: HazeBuster -> Generate Pixel Art Placeholders
    /// </summary>
    public static class PixelArtGenerator
    {
        // Files written by the current run. Only these get the pixel-art import settings, so your own
        // art in the same folder is never touched.
        private static readonly List<string> written = new List<string>();
        private static bool overwriteExisting;

        [MenuItem("HazeBuster/Generate Pixel Art Placeholders")]
        public static void GenerateAllSprites()
        {
            overwriteExisting = false;
            GenerateAll();
        }

        [MenuItem("HazeBuster/Generate Pixel Art Placeholders (Overwrite All)")]
        public static void GenerateAllSpritesOverwrite()
        {
            if (!EditorUtility.DisplayDialog("HAZEBUSTER", "This replaces EVERY placeholder in Assets/Sprites, including any you have already swapped for your own art that uses the same file name. Continue?", "Overwrite", "Cancel"))
            {
                return;
            }
            overwriteExisting = true;
            GenerateAll();
        }

        private static void GenerateAll()
        {
            written.Clear();
            string folderPath = "Assets/Sprites";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            SaveSprite(CreateScientistSprite(), folderPath + "/ScientistPlayer.png");
            SaveSprite(CreateWaterDropletSprite(), folderPath + "/WaterDroplet.png");
            SaveSprite(CreateDewPickupSprite(), folderPath + "/DewPickup.png");
            SaveSprite(CreateEmberSprite(), folderPath + "/EmberEnemy.png");
            SaveSprite(CreateBurnedStumpSprite(), folderPath + "/BurnedStumpEnemy.png");
            SaveSprite(CreateGambutSprite(), folderPath + "/GambutEnemy.png");
            SaveSprite(CreateFireBossSprite(), folderPath + "/FireBoss.png");
            SaveSprite(CreateBurningGroundSprite(), folderPath + "/BurningGround.png");
            SaveSprite(CreateFoamBombSprite(), folderPath + "/FoamBomb.png");
            SaveSprite(CreateFoamPuddleSprite(), folderPath + "/FoamPuddle.png");
            SaveSprite(CreatePeatGroundSprite(), folderPath + "/PeatGround.png");

            AssetDatabase.Refresh();
            ConfigureImportSettings(folderPath);

            string summary = written.Count + " placeholder sprite(s) written to Assets/Sprites." +
                             (written.Count < 11 ? " Existing files were kept, so your own art is safe." : "");
            EditorUtility.DisplayDialog("HAZEBUSTER", summary, "Awesome!");
            Debug.Log("<color=green>[HAZEBUSTER] " + summary + "</color>");
        }

        private static void SaveSprite(Texture2D tex, string path)
        {
            // Never overwrite a file that is already there (it may be your own art).
            if (File.Exists(path) && !overwriteExisting)
            {
                return;
            }

            byte[] pngData = tex.EncodeToPNG();
            File.WriteAllBytes(path, pngData);
            written.Add(path);
        }

        private static void ConfigureImportSettings(string folderPath)
        {
            string[] files = written.ToArray(); // only the files written in this run
            foreach (string file in files)
            {
                TextureImporter importer = AssetImporter.GetAtPath(file) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.filterMode = FilterMode.Point; // Crisp pixel art
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }
            }
        }

        // 1. Scientist Engineer (16x16 pixel art)
        public static Texture2D CreateScientistSprite()
        {
            Texture2D tex = CreateBaseTexture(16, 16);
            Color suit = new Color(0.15f, 0.45f, 0.4f, 1f); // Forest green/teal Hazmat suit
            Color visor = new Color(0.9f, 0.8f, 0.2f, 1f); // Yellow visor
            Color tank = new Color(0.2f, 0.6f, 0.95f, 1f); // Blue water tank
            Color boots = new Color(0.1f, 0.1f, 0.15f, 1f);

            // Head & Visor
            FillRect(tex, 6, 10, 4, 4, suit);
            FillRect(tex, 7, 11, 2, 2, visor);

            // Torso & Suit
            FillRect(tex, 5, 5, 6, 5, suit);

            // Backpack Water Tank
            FillRect(tex, 3, 6, 2, 4, tank);

            // Legs / Boots
            FillRect(tex, 5, 2, 2, 3, boots);
            FillRect(tex, 9, 2, 2, 3, boots);

            tex.Apply();
            return tex;
        }

        // 2. Water Droplet Projectile (12x12)
        public static Texture2D CreateWaterDropletSprite()
        {
            Texture2D tex = CreateBaseTexture(12, 12);
            Color water = new Color(0.15f, 0.75f, 1f, 1f);
            Color highlight = new Color(0.7f, 0.95f, 1f, 1f);

            FillCircle(tex, 6, 5, 4, water);
            FillRect(tex, 5, 8, 2, 2, water);
            tex.SetPixel(6, 10, water);

            // Glisten highlight
            tex.SetPixel(5, 6, highlight);
            tex.SetPixel(5, 7, highlight);

            tex.Apply();
            return tex;
        }

        // 3. Morning Dew EXP Pickup (12x12)
        public static Texture2D CreateDewPickupSprite()
        {
            Texture2D tex = CreateBaseTexture(12, 12);
            Color dewGlow = new Color(0.3f, 1f, 0.8f, 1f); // Aquamarine glow
            Color dewCore = new Color(0.85f, 1f, 0.95f, 1f);

            FillCircle(tex, 6, 6, 4, dewGlow);
            FillCircle(tex, 6, 6, 2, dewCore);

            tex.Apply();
            return tex;
        }

        // 4. Airborne Ember Enemy (12x12)
        public static Texture2D CreateEmberSprite()
        {
            Texture2D tex = CreateBaseTexture(12, 12);
            Color fireRed = new Color(0.95f, 0.2f, 0.1f, 1f);
            Color fireOrange = new Color(1f, 0.6f, 0.1f, 1f);
            Color sparkYellow = new Color(1f, 0.95f, 0.4f, 1f);

            FillCircle(tex, 6, 6, 4, fireRed);
            FillCircle(tex, 6, 6, 3, fireOrange);
            FillCircle(tex, 6, 6, 1, sparkYellow);

            tex.Apply();
            return tex;
        }

        // 5. Burned Stump Enemy (20x20)
        public static Texture2D CreateBurnedStumpSprite()
        {
            Texture2D tex = CreateBaseTexture(20, 20);
            Color charredBark = new Color(0.18f, 0.12f, 0.1f, 1f);
            Color emberGlow = new Color(1f, 0.35f, 0.05f, 1f);

            // Trunk block
            FillRect(tex, 4, 3, 12, 13, charredBark);

            // Root flares
            FillRect(tex, 2, 2, 3, 3, charredBark);
            FillRect(tex, 15, 2, 3, 3, charredBark);

            // Glowing hot cracks
            FillRect(tex, 7, 7, 2, 5, emberGlow);
            FillRect(tex, 11, 5, 2, 7, emberGlow);
            FillRect(tex, 8, 14, 4, 2, emberGlow);

            tex.Apply();
            return tex;
        }

        // 6. Gambut Peat Monster (18x18)
        public static Texture2D CreateGambutSprite()
        {
            Texture2D tex = CreateBaseTexture(18, 18);
            Color peatSoil = new Color(0.22f, 0.16f, 0.12f, 1f);
            Color smolderingEye = new Color(1f, 0.25f, 0.05f, 1f);

            // Slime / subterranean blob
            FillCircle(tex, 9, 7, 7, peatSoil);

            // Glowing subterranean eyes
            tex.SetPixel(7, 8, smolderingEye);
            tex.SetPixel(8, 8, smolderingEye);
            tex.SetPixel(11, 8, smolderingEye);
            tex.SetPixel(12, 8, smolderingEye);

            tex.Apply();
            return tex;
        }

        // 7. Mega Fire Boss (32x32)
        public static Texture2D CreateFireBossSprite()
        {
            Texture2D tex = CreateBaseTexture(32, 32);
            Color outerFire = new Color(0.85f, 0.1f, 0.05f, 1f);
            Color midFire = new Color(1f, 0.5f, 0.1f, 1f);
            Color coreFire = new Color(1f, 0.95f, 0.3f, 1f);
            Color darkHorns = new Color(0.12f, 0.08f, 0.08f, 1f);

            // Colossal flame body
            FillCircle(tex, 16, 14, 12, outerFire);
            FillCircle(tex, 16, 14, 9, midFire);
            FillCircle(tex, 16, 14, 5, coreFire);

            // Charred root horns
            FillRect(tex, 8, 22, 3, 7, darkHorns);
            FillRect(tex, 21, 22, 3, 7, darkHorns);

            // Glowing eyes
            FillRect(tex, 12, 15, 2, 3, Color.white);
            FillRect(tex, 18, 15, 2, 3, Color.white);

            tex.Apply();
            return tex;
        }

        // 8. Burning Ground (16x16)
        public static Texture2D CreateBurningGroundSprite()
        {
            Texture2D tex = CreateBaseTexture(16, 16);
            Color baseSoil = new Color(0.25f, 0.15f, 0.1f, 0.8f);
            Color hotCracks = new Color(1f, 0.4f, 0.05f, 0.9f);

            FillCircle(tex, 8, 8, 7, baseSoil);
            FillRect(tex, 5, 7, 6, 2, hotCracks);
            FillRect(tex, 7, 4, 2, 8, hotCracks);

            tex.Apply();
            return tex;
        }

        // 9. Foam Bomb Canister (12x12)
        public static Texture2D CreateFoamBombSprite()
        {
            Texture2D tex = CreateBaseTexture(12, 12);
            Color canister = new Color(0.85f, 0.85f, 0.9f, 1f);
            Color band = new Color(0.2f, 0.7f, 1f, 1f);

            FillRect(tex, 3, 3, 6, 7, canister);
            FillRect(tex, 3, 5, 6, 2, band);

            tex.Apply();
            return tex;
        }

        // 10. Foam Puddle (24x24)
        public static Texture2D CreateFoamPuddleSprite()
        {
            Texture2D tex = CreateBaseTexture(24, 24);
            Color foam = new Color(0.85f, 0.95f, 1f, 0.75f);
            Color bubble = new Color(0.6f, 0.85f, 1f, 0.9f);

            FillCircle(tex, 12, 12, 10, foam);
            FillCircle(tex, 9, 13, 3, bubble);
            FillCircle(tex, 15, 10, 4, bubble);

            tex.Apply();
            return tex;
        }

        // 11. Peat Ground Tile (16x16)
        public static Texture2D CreatePeatGroundSprite()
        {
            Texture2D tex = CreateBaseTexture(16, 16);
            Color basePeat = new Color(0.17f, 0.12f, 0.09f, 1f);
            Color darkFibers = new Color(0.12f, 0.08f, 0.06f, 1f);
            Color lightDirt = new Color(0.24f, 0.17f, 0.13f, 1f);
            Color mossSpeck = new Color(0.21f, 0.27f, 0.15f, 1f);

            FillRect(tex, 0, 0, 16, 16, basePeat);
            tex.SetPixel(2, 3, darkFibers); tex.SetPixel(3, 3, darkFibers);
            tex.SetPixel(10, 8, darkFibers); tex.SetPixel(11, 8, darkFibers);
            tex.SetPixel(7, 5, lightDirt); tex.SetPixel(13, 3, lightDirt);
            tex.SetPixel(4, 9, mossSpeck); tex.SetPixel(11, 2, mossSpeck);

            tex.Apply();
            return tex;
        }

        // --- Helper Drawing Functions ---
        private static Texture2D CreateBaseTexture(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color transparent = new Color(0, 0, 0, 0);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    tex.SetPixel(x, y, transparent);
                }
            }
            return tex;
        }

        private static void FillRect(Texture2D tex, int startX, int startY, int width, int height, Color color)
        {
            for (int y = startY; y < startY + height; y++)
            {
                for (int x = startX; x < startX + width; x++)
                {
                    if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                    {
                        tex.SetPixel(x, y, color);
                    }
                }
            }
        }

        private static void FillCircle(Texture2D tex, int cx, int cy, int radius, Color color)
        {
            for (int y = cy - radius; y <= cy + radius; y++)
            {
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    if (x >= 0 && x < tex.width && y >= 0 && y < tex.height)
                    {
                        if (Vector2.Distance(new Vector2(cx, cy), new Vector2(x, y)) <= radius)
                        {
                            tex.SetPixel(x, y, color);
                        }
                    }
                }
            }
        }
    }
}
