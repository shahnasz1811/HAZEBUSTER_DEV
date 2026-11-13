using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using HazeBuster.Art;

namespace HazeBuster.Editor
{
    /// <summary>
    /// One-click setup for replaceable art. Access via: HazeBuster > Art
    ///
    /// Set Up Replaceable Art
    ///   Creates Assets/Resources/HazeBusterArt.asset (the Art Library) if it is missing, adds any
    ///   slot it does not have yet, and puts an ArtSlot on every prefab and scene object that needs
    ///   one (Player, enemies, pickups, projectiles). Safe to run again: nothing you typed into the
    ///   library is ever overwritten.
    ///
    /// Prepare Selected Texture As Ground
    ///   Sets the import settings a seamless ground picture needs (repeat, mip maps, smooth).
    /// </summary>
    public static class ArtSetupTool
    {
        private const string LibraryPath = "Assets/Resources/HazeBusterArt.asset";

        private class StartValues
        {
            public readonly ArtId id;
            public readonly float height;
            public readonly float hitbox;
            public readonly bool shadow;

            public StartValues(ArtId id, float height, float hitbox, bool shadow)
            {
                this.id = id;
                this.height = height;
                this.hitbox = hitbox;
                this.shadow = shadow;
            }
        }

        // The starting sizes, in WORLD UNITS. The player is about 1 unit tall and everything else is
        // sized against that, which is the scale the game's speeds, ranges and colliders were
        // written for. 0 = leave that value as it is.
        private static readonly StartValues[] Starts = new StartValues[]
        {
            new StartValues(ArtId.Player,         1.0f,  0.28f, true),
            new StartValues(ArtId.Ember,          0.75f, 0.30f, true),
            new StartValues(ArtId.BurnedStump,    1.2f,  0.50f, true),
            new StartValues(ArtId.Gambut,         1.1f,  0.45f, true),
            new StartValues(ArtId.FireBoss,       2.6f,  1.00f, true),
            new StartValues(ArtId.DewPickup,      0.45f, 0.25f, false),
            new StartValues(ArtId.WaterDroplet,   0.4f,  0.15f, false),
            new StartValues(ArtId.FireProjectile, 0.7f,  0.25f, false),
            new StartValues(ArtId.FoamBomb,       0.5f,  0f,    false),
            new StartValues(ArtId.FoamPuddle,     0f,    0f,    false),
            new StartValues(ArtId.BurningGround,  0f,    0f,    false)
        };

        [MenuItem("HazeBuster/Art/Set Up Replaceable Art")]
        public static void SetUp()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("HAZEBUSTER", "Stop Play mode first, then run this again.", "OK");
                return;
            }

            ArtLibrary library = GetOrCreateLibrary();
            int prefabs = AttachToPrefabs();
            int sceneObjects = AttachToOpenScene();

            Selection.activeObject = library;
            EditorGUIUtility.PingObject(library);

            string message =
                "Art Library ready: Assets/Resources/HazeBusterArt.asset (selected for you).\n\n" +
                "Art Slots added to " + prefabs + " prefab(s) and " + sceneObjects + " scene object(s).\n\n" +
                "TO REPLACE A PLACEHOLDER: drag your sprite into its slot in the library and press Play. " +
                "It is scaled to the slot's World Height, so any image size fits.\n\n" +
                "Save the scene now (Ctrl+S).";
            EditorUtility.DisplayDialog("HAZEBUSTER", message, "Awesome!");
            Debug.Log("<color=green>[HAZEBUSTER] Replaceable art is set up. Library: " + LibraryPath +
                      ". Slots added: " + prefabs + " prefab(s), " + sceneObjects + " scene object(s).</color>");
        }

        [MenuItem("HazeBuster/Art/Prepare Selected Texture As Ground")]
        public static void PrepareGroundTexture()
        {
            int count = 0;
            Object[] selected = Selection.objects;
            for (int i = 0; i < selected.Length; i++)
            {
                string path = AssetDatabase.GetAssetPath(selected[i]);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                importer.textureType = TextureImporterType.Default;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.filterMode = FilterMode.Bilinear;
                importer.mipmapEnabled = true;
                importer.anisoLevel = 8;
                importer.sRGBTexture = true;
                importer.SaveAndReimport();
                count++;
            }

            string message = count > 0
                ? count + " texture(s) prepared as ground (repeat, mip maps, smooth). Drag one into the Art Library > Ground Texture slot."
                : "Select your ground picture in the Project window first, then run this again.";
            EditorUtility.DisplayDialog("HAZEBUSTER", message, "OK");
        }

        // ---------------------------------------------------------------- library

        private static ArtLibrary GetOrCreateLibrary()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            ArtLibrary library = AssetDatabase.LoadAssetAtPath<ArtLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<ArtLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }

            // Add the slots that are missing. Slots that exist, and anything typed in them, are never touched.
            List<ArtLibrary.Slot> slots = new List<ArtLibrary.Slot>();
            if (library.slots != null)
            {
                slots.AddRange(library.slots);
            }

            for (int s = 0; s < Starts.Length; s++)
            {
                bool exists = false;
                for (int i = 0; i < slots.Count; i++)
                {
                    if (slots[i] != null && slots[i].id == Starts[s].id)
                    {
                        exists = true;
                        break;
                    }
                }
                if (exists) continue;

                ArtLibrary.Slot slot = new ArtLibrary.Slot();
                slot.id = Starts[s].id;
                slot.worldHeight = Starts[s].height;
                slot.hitboxRadius = Starts[s].hitbox;
                slot.contactShadow = Starts[s].shadow;
                slots.Add(slot);
            }

            library.slots = slots.ToArray();
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            return library;
        }

        // ---------------------------------------------------------------- slots on objects

        private static int AttachToPrefabs()
        {
            int added = 0;
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new string[] { "Assets" });
            for (int g = 0; g < guids.Length; g++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[g]);
                GameObject root = null;
                try
                {
                    root = PrefabUtility.LoadPrefabContents(path);

                    ArtId id;
                    if (TryFindId(root, out id) &&
                        root.GetComponent<ArtSlot>() == null &&
                        root.GetComponentInChildren<SpriteRenderer>(true) != null)
                    {
                        ArtSlot slot = root.AddComponent<ArtSlot>();
                        slot.id = id;
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        added++;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[HAZEBUSTER] Skipped prefab " + path + ": " + e.Message);
                }
                finally
                {
                    if (root != null)
                    {
                        PrefabUtility.UnloadPrefabContents(root);
                    }
                }
            }
            return added;
        }

        private static int AttachToOpenScene()
        {
            int added = 0;
            MonoBehaviour[] all = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                MonoBehaviour behaviour = all[i];
                if (behaviour == null) continue;

                ArtId id;
                if (!TryIdFromTypeName(behaviour.GetType().Name, out id)) continue;

                GameObject go = behaviour.gameObject;
                if (go.GetComponent<ArtSlot>() != null) continue;
                if (go.GetComponentInChildren<SpriteRenderer>(true) == null) continue;

                ArtSlot slot = Undo.AddComponent<ArtSlot>(go);
                slot.id = id;
                added++;
            }

            if (added > 0)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
            return added;
        }

        private static bool TryFindId(GameObject root, out ArtId id)
        {
            MonoBehaviour[] behaviours = root.GetComponents<MonoBehaviour>();
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] == null) continue; // a missing script
                if (TryIdFromTypeName(behaviours[i].GetType().Name, out id))
                {
                    return true;
                }
            }
            id = ArtId.Player;
            return false;
        }

        // Matches by class name, so this tool does not depend on which namespace a script lives in.
        private static bool TryIdFromTypeName(string typeName, out ArtId id)
        {
            switch (typeName)
            {
                case "PlayerController": id = ArtId.Player; return true;
                case "EmberEnemy": id = ArtId.Ember; return true;
                case "BurnedStumpEnemy": id = ArtId.BurnedStump; return true;
                case "GambutEnemy": id = ArtId.Gambut; return true;
                case "FireBoss": id = ArtId.FireBoss; return true;
                case "DewPickup": id = ArtId.DewPickup; return true;
                case "WaterProjectile": id = ArtId.WaterDroplet; return true;
                case "FireProjectile": id = ArtId.FireProjectile; return true;
                case "FoamBomb":
                case "FoamBombProjectile": id = ArtId.FoamBomb; return true;
                case "FoamPuddle": id = ArtId.FoamPuddle; return true;
                case "BurningGround": id = ArtId.BurningGround; return true;
                default:
                    id = ArtId.Player;
                    return false;
            }
        }
    }
}
