using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace HazeBuster.Editor
{
    /// <summary>
    /// Writes a balanced starting set of speeds into the Player (open scene) and the enemy prefabs.
    /// Access via: HazeBuster > Balance > Apply Hell Maiden Starting Feel
    ///
    /// WHY IT EXISTS
    /// The enemies used to force their speed, health and damage every time they spawned, so numbers
    /// typed into the Inspector did nothing. That is fixed in code now, which means the numbers
    /// saved on the prefabs finally count, and some of them are old test values. This tool replaces
    /// them with a set that works together with the 1-unit-tall characters from the Art Library.
    ///
    /// The ratio matters more than the numbers: the Player is fastest, an Ember (the fast swarmer)
    /// is about two thirds of the Player's speed, a Gambut about half, a Stump about a third.
    /// After running it, tune any number in the Inspector as usual.
    /// </summary>
    public static class FeelSetupTool
    {
        private const float PlayerMoveSpeed = 4.5f;
        private const float PlayerDashSpeed = 13f;

        private class EnemyTune
        {
            public readonly string typeName;
            public readonly string label;
            public readonly float health;
            public readonly float speed;
            public readonly float damage;
            public readonly int dewAmount;
            public readonly string dewTier;

            public EnemyTune(string typeName, string label, float health, float speed, float damage, int dewAmount, string dewTier)
            {
                this.typeName = typeName;
                this.label = label;
                this.health = health;
                this.speed = speed;
                this.damage = damage;
                this.dewAmount = dewAmount;
                this.dewTier = dewTier;
            }
        }

        // Health, damage and dew are the values the enemies were really using until now (the ones
        // forced in code), so only the speeds change the game.
        private static readonly EnemyTune[] Tunes = new EnemyTune[]
        {
            new EnemyTune("EmberEnemy", "Ember", 15f, 3.0f, 10f, 1, "Small"),
            new EnemyTune("BurnedStumpEnemy", "Burned Stump", 80f, 1.4f, 25f, 1, "Large"),
            new EnemyTune("GambutEnemy", "Gambut", 45f, 2.0f, 18f, 1, "Medium")
        };

        [MenuItem("HazeBuster/Balance/Apply Hell Maiden Starting Feel")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("HAZEBUSTER", "Stop Play mode first, then run this again.", "OK");
                return;
            }

            string plan =
                "This will set:\n\n" +
                "Player (open scene)\n" +
                "  Move speed " + PlayerMoveSpeed + ", dash speed " + PlayerDashSpeed + "\n\n" +
                "Enemy prefabs (speed / health / contact damage)\n";
            for (int i = 0; i < Tunes.Length; i++)
            {
                plan += "  " + Tunes[i].label + ": " + Tunes[i].speed + " / " + Tunes[i].health + " / " + Tunes[i].damage + "\n";
            }
            plan += "\nYour current values for these numbers are replaced. Continue?";

            if (!EditorUtility.DisplayDialog("HAZEBUSTER", plan, "Apply", "Cancel"))
            {
                return;
            }

            int players = TunePlayer();
            int prefabs = TuneEnemyPrefabs();

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("HAZEBUSTER",
                "Done.\n\nPlayer updated: " + players + "\nEnemy prefabs updated: " + prefabs + "\n\nSave the scene now (Ctrl+S).",
                "Awesome!");
            Debug.Log("<color=green>[HAZEBUSTER] Starting feel applied. Player: " + players + ", enemy prefabs: " + prefabs + ".</color>");
        }

        // ---------------------------------------------------------------- player

        private static int TunePlayer()
        {
            int changed = 0;
            MonoBehaviour[] all = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                MonoBehaviour behaviour = all[i];
                if (behaviour == null || behaviour.GetType().Name != "PlayerController") continue;

                SerializedObject so = new SerializedObject(behaviour);
                SetFloat(so, "baseMoveSpeed", PlayerMoveSpeed);
                SetFloat(so, "currentMoveSpeed", PlayerMoveSpeed);
                SetFloat(so, "dashSpeed", PlayerDashSpeed);
                so.ApplyModifiedProperties();
                changed++;
            }

            if (changed > 0)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
            return changed;
        }

        // ---------------------------------------------------------------- enemy prefabs

        private static int TuneEnemyPrefabs()
        {
            int changed = 0;
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new string[] { "Assets" });
            for (int g = 0; g < guids.Length; g++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[g]);
                GameObject root = null;
                try
                {
                    root = PrefabUtility.LoadPrefabContents(path);
                    bool modified = false;

                    MonoBehaviour[] behaviours = root.GetComponents<MonoBehaviour>();
                    for (int b = 0; b < behaviours.Length; b++)
                    {
                        if (behaviours[b] == null) continue;

                        EnemyTune tune = FindTune(behaviours[b].GetType().Name);
                        if (tune == null) continue;

                        SerializedObject so = new SerializedObject(behaviours[b]);
                        SetFloat(so, "maxHealth", tune.health);
                        SetFloat(so, "moveSpeed", tune.speed);
                        SetFloat(so, "contactDamage", tune.damage);

                        SerializedProperty dew = so.FindProperty("dewDropAmount");
                        if (dew != null) dew.intValue = tune.dewAmount;

                        SerializedProperty tier = so.FindProperty("dewDropTier");
                        if (tier != null) SetEnumByName(tier, tune.dewTier);

                        so.ApplyModifiedProperties();
                        modified = true;
                    }

                    if (modified)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        changed++;
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
            return changed;
        }

        private static EnemyTune FindTune(string typeName)
        {
            for (int i = 0; i < Tunes.Length; i++)
            {
                if (Tunes[i].typeName == typeName) return Tunes[i];
            }
            return null;
        }

        // ---------------------------------------------------------------- helpers

        private static void SetFloat(SerializedObject so, string propertyName, float value)
        {
            SerializedProperty property = so.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        private static void SetEnumByName(SerializedProperty property, string name)
        {
            string[] names = property.enumNames;
            for (int i = 0; i < names.Length; i++)
            {
                if (names[i] == name)
                {
                    property.enumValueIndex = i;
                    return;
                }
            }
        }
    }
}
