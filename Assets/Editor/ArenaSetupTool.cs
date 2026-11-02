using UnityEngine;
using UnityEditor;

namespace HazeBuster.Editor
{
    /// <summary>
    /// Helper tool for Art & Design students.
    /// Automatically builds the 4 perimeter walls around the 60x60 peatland arena with 1 click!
    /// Access via: HazeBuster -> Build Arena Walls (60x60)
    /// </summary>
    public static class ArenaSetupTool
    {
        [MenuItem("HazeBuster/Build Arena Walls (60x60)")]
        public static void BuildWalls()
        {
            // 1. Create or find the root Boundaries object
            GameObject boundaries = GameObject.Find("Arena_Boundaries");
            if (boundaries == null)
            {
                boundaries = new GameObject("Arena_Boundaries");
                boundaries.transform.position = Vector3.zero;
            }

            // Remove existing walls if re-running
            while (boundaries.transform.childCount > 0)
            {
                Object.DestroyImmediate(boundaries.transform.GetChild(0).gameObject);
            }

            float halfSize = HazeBuster.Utility.ArenaBounds.halfSize; // one shared arena size for walls and scripts
            float thickness = 2f;
            float totalSpan = (halfSize * 2f) + (thickness * 2f); // 64f to seal corners completely

            // North Wall (Top)
            CreateWall(boundaries.transform, "Wall_North", new Vector2(0f, halfSize + (thickness / 2f)), new Vector2(totalSpan, thickness));

            // South Wall (Bottom)
            CreateWall(boundaries.transform, "Wall_South", new Vector2(0f, -halfSize - (thickness / 2f)), new Vector2(totalSpan, thickness));

            // East Wall (Right)
            CreateWall(boundaries.transform, "Wall_East", new Vector2(halfSize + (thickness / 2f), 0f), new Vector2(thickness, totalSpan));

            // West Wall (Left)
            CreateWall(boundaries.transform, "Wall_West", new Vector2(-halfSize - (thickness / 2f), 0f), new Vector2(thickness, totalSpan));

            Selection.activeGameObject = boundaries;
            EditorUtility.DisplayDialog("HAZEBUSTER", "Success! 4 invisible collision walls have been built around your 60x60 Peatland Arena!", "Great!");
            Debug.Log("<color=green>[HAZEBUSTER] 4 Arena Walls created around 60x60 boundary!</color>");
        }

        private static void CreateWall(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            GameObject wall = new GameObject(name);
            wall.transform.SetParent(parent);
            wall.transform.localPosition = new Vector3(pos.x, pos.y, 0f);

            BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
            col.size = size;
            col.isTrigger = false; // Solid physical wall
        }
    }
}
