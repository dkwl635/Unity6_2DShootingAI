// Attach to: (utility class — instantiate via code, no direct GameObject attachment)

using System.Collections.Generic;
using UnityEngine;

namespace ShooterGame.Utils
{
    /// <summary>
    /// Generic object pool. Manages a queue of pre-instantiated GameObjects.
    /// Usage: create an ObjectPool<T> in Awake(), then call Get() / Release().
    /// </summary>
    public class ObjectPool<T> where T : Component
    {
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly Queue<T> _queue = new Queue<T>();

        public ObjectPool(T prefab, int initialSize, Transform parent = null)
        {
            _prefab = prefab;
            _parent = parent;

            // Pre-warm the pool
            for (int i = 0; i < initialSize; i++)
            {
                T obj = CreateNew();
                obj.gameObject.SetActive(false);
                _queue.Enqueue(obj);
            }
        }

        /// <summary>Returns a pooled object, activating it for use.</summary>
        public T Get()
        {
            T obj = _queue.Count > 0 ? _queue.Dequeue() : CreateNew();
            obj.gameObject.SetActive(true);
            return obj;
        }

        /// <summary>Returns an object back to the pool, deactivating it.</summary>
        public void Release(T obj)
        {
            obj.gameObject.SetActive(false);
            _queue.Enqueue(obj);
        }

        /// <summary>Returns all active objects back to the pool (call before scene unload).</summary>
        public void ReleaseAll(IEnumerable<T> activeObjects)
        {
            foreach (T obj in activeObjects)
            {
                if (obj != null && obj.gameObject.activeSelf)
                    Release(obj);
            }
        }

        private T CreateNew()
        {
            T obj = Object.Instantiate(_prefab, _parent);
            obj.gameObject.SetActive(false);
            return obj;
        }
    }
}
