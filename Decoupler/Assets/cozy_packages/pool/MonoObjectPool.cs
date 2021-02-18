using System.Collections.Generic;
using UnityEngine;

namespace com.cozyhome.Pooling
{
    public class MonoObjectPool<T> : MonoBehaviour where T : MonoBehaviour
    {
        [Header("Pool Settings")]
        [SerializeField] private GameObject ObjectArchetype;
        [SerializeField] private int Capacity = 10;
        [SerializeField] private bool GenerateOnDiscovery = true;

        [System.NonSerialized] private bool IsInitialized = false;
        [System.NonSerialized] private Queue<GameObject> FreeObjects = new Queue<GameObject>();

        public void OnDiscovery()
        {
            if (GenerateOnDiscovery &&
                !IsInitialized)
            {
                if (!ObjectArchetype.GetComponent<T>()) // if is not of archetype type, get mad
                {
                    Debug.LogError("Object pool does not contain prefab with correct type specified");
                }
                else
                {
                    // Generate ()
                    IsInitialized = true;

                    int i = 0;
                    while (i < Capacity)
                    {
                        FreeObjects.Enqueue(GameObject.Instantiate(ObjectArchetype,
                                            Vector3.zero,
                                            Quaternion.identity,
                                            null));
                        i++;
                    }
                }

            }
            else
                return;
        }

        public bool Grab(out GameObject obj)
        {
            obj = null;
            if (FreeObjects.Count > 0)
            {
                obj = FreeObjects.Dequeue();
                return true;
            }
            else
                return false;
        }

        public bool Release(GameObject obj)
        {
            if (FreeObjects.Count < Capacity &&
                obj.GetComponent<T>()) // is this object the right type?
            {
                FreeObjects.Enqueue(obj);
                return true;
            }
            else
                return false;
        }
    }
}