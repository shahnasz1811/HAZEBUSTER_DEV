using UnityEngine;
using UnityEditor;
using HazeBuster.Utility;

namespace HazeBuster.Editor
{
    /// <summary>
    /// Preview the slanted orthographic camera in the Scene without pressing Play.
    /// HazeBuster -> Camera -> Apply Slanted Orthographic View
    /// HazeBuster -> Camera -> Reset To Flat 2D View
    ///
    /// The tilt, distance and everything else can then be tuned on the camera's CameraFollow component.
    /// The game applies the same view by itself on Play, so running these is optional.
    /// </summary>
    public static class SlantedPerspectiveTool
    {
        [MenuItem("HazeBuster/Camera/Apply Slanted Orthographic View")]
        public static void ApplySlanted()
        {
            Apply(true);
        }

        [MenuItem("HazeBuster/Camera/Reset To Flat 2D View")]
        public static void ApplyFlat()
        {
            Apply(false);
        }

        private static void Apply(bool slanted)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                EditorUtility.DisplayDialog("HAZEBUSTER", "No camera tagged MainCamera was found in the open scene.", "OK");
                return;
            }

            CameraFollow follow = cam.GetComponent<CameraFollow>();
            if (follow == null)
            {
                follow = Undo.AddComponent<CameraFollow>(cam.gameObject);
            }

            Undo.RecordObject(follow, "Camera View");
            Undo.RecordObject(cam.transform, "Camera View");
            Undo.RecordObject(cam, "Camera View");

            follow.slantedView = slanted;
            follow.SnapToTarget();

            EditorUtility.SetDirty(follow);
            EditorUtility.SetDirty(cam.transform);
            EditorUtility.SetDirty(cam);
            Selection.activeGameObject = cam.gameObject;

            Debug.Log(slanted
                ? "<color=green>[HAZEBUSTER] Slanted orthographic camera applied. Tune Tilt Angle on the camera's CameraFollow. Characters only stand upright while playing.</color>"
                : "<color=green>[HAZEBUSTER] Camera reset to the flat 2D view.</color>");
        }
    }
}
