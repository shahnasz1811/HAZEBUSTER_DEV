using UnityEditor;
using UnityEngine;

namespace HazeBuster.Editor
{
    /// <summary>
    /// Makes sure the tags the game scripts rely on ("Enemy" and "Hazard") exist.
    /// Runs by itself whenever Unity reloads scripts, and can also be run by hand from
    /// HazeBuster > Ensure Required Tags. The built-in "Player" tag always exists.
    /// </summary>
    [InitializeOnLoad]
    public static class TagSetup
    {
        private static readonly string[] RequiredTags = { "Enemy", "Hazard" };

        static TagSetup()
        {
            // Wait until the editor has finished loading before touching project settings.
            EditorApplication.delayCall += EnsureTags;
        }

        [MenuItem("HazeBuster/Ensure Required Tags")]
        public static void EnsureTags()
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return;

            SerializedObject tagManager = new SerializedObject(assets[0]);
            SerializedProperty tags = tagManager.FindProperty("tags");
            if (tags == null) return;

            bool changed = false;
            foreach (string required in RequiredTags)
            {
                if (HasTag(tags, required)) continue;

                int index = tags.arraySize;
                tags.InsertArrayElementAtIndex(index);
                tags.GetArrayElementAtIndex(index).stringValue = required;
                changed = true;
                Debug.Log("[HAZEBUSTER] Added missing tag: " + required);
            }

            if (changed)
            {
                tagManager.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
            }
        }

        private static bool HasTag(SerializedProperty tags, string tag)
        {
            for (int i = 0; i < tags.arraySize; i++)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == tag) return true;
            }
            return false;
        }
    }
}
