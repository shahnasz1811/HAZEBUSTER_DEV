using System.Collections.Generic;
using UnityEngine;

namespace HazeBuster.Utility
{
    /// <summary>
    /// Reuses frequently created objects (water droplets, fireballs) instead of creating and
    /// destroying them all the time, which keeps the garbage collector quiet during swarms.
    ///
    /// Use PoolManager.Spawn(prefab, position, rotation) instead of Instantiate, and
    /// PoolManager.Despawn(gameObject) instead of Destroy. Anything the pool did not create is
    /// simply destroyed by Despawn, so mixing the two is safe.
    ///
    /// A reused object is NOT reset automatically. The caller must reset its own state after
    /// Spawn (for projectiles that is their Initialize method).
    ///
    /// The pool lives in the current scene: a Retry reloads the scene and throws the pool away.
    /// </summary>
    public class PoolManager : MonoBehaviour
    {
        private static PoolManager instance;

        // Inactive objects waiting to be reused, per prefab.
        private readonly Dictionary<GameObject, Stack<GameObject>> idle = new Dictionary<GameObject, Stack<GameObject>>();

        // Which prefab each pooled object came from.
        private readonly Dictionary<GameObject, GameObject> prefabOf = new Dictionary<GameObject, GameObject>();

        private static PoolManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject root = new GameObject("_PoolManager");
                    instance = root.AddComponent<PoolManager>();
                }
                return instance;
            }
        }

        /// <summary>
        /// Returns an active copy of the prefab at the given position, reusing an idle one if possible.
        /// </summary>
        public static GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null) return null;
            return Instance.SpawnFromPool(prefab, position, rotation);
        }

        /// <summary>
        /// Gives an object back to the pool. Objects the pool did not create are destroyed instead.
        /// </summary>
        public static void Despawn(GameObject obj)
        {
            if (obj == null) return;

            if (instance == null)
            {
                Destroy(obj);
                return;
            }
            instance.ReturnToPool(obj);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        private GameObject SpawnFromPool(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            Stack<GameObject> stack;
            if (!idle.TryGetValue(prefab, out stack))
            {
                stack = new Stack<GameObject>();
                idle[prefab] = stack;
            }

            // Skip any idle object that something else destroyed in the meantime.
            GameObject obj = null;
            while (obj == null && stack.Count > 0)
            {
                obj = stack.Pop();
            }

            if (obj == null)
            {
                obj = Instantiate(prefab, position, rotation, transform);
                prefabOf[obj] = prefab;
                return obj;
            }

            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            return obj;
        }

        private void ReturnToPool(GameObject obj)
        {
            GameObject prefab;
            if (!prefabOf.TryGetValue(obj, out prefab))
            {
                Destroy(obj); // not created by the pool
                return;
            }

            // Already idle. Returning it twice would hand one object to two users.
            if (!obj.activeSelf) return;

            obj.SetActive(false);
            idle[prefab].Push(obj);
        }
    }
}
