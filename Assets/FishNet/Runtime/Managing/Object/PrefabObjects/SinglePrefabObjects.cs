using FishNet.Object;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FishNet.Managing.Object
{

    [CreateAssetMenu(fileName = "New SinglePrefabObjects", menuName = "FishNet/Spawnable Prefabs/Single Prefab Objects")]
    public class SinglePrefabObjects : PrefabObjects
    {
        [SerializeField]
        private List<NetworkObject> _prefabs = new List<NetworkObject>();

        public override void Clear()
        {
            _prefabs.Clear();
        }
        public override int GetObjectCount()
        {
            return _prefabs.Count;
        }
        public override NetworkObject GetObject(bool asServer, int id)
        {
            if (id < 0 || id >= _prefabs.Count)
            {
                Debug.LogError($"PrefabId {id} is out of range.");
                return null;
            }
            else
            {
                NetworkObject nob = _prefabs[id];
                if (nob == null)
                    Debug.LogError($"Prefab on id {id} is null.");

                return nob;
            }
        }

        public override void RemoveNull()
        {
            for (int i = 0; i < _prefabs.Count; i++)
            {
                if (_prefabs[i] == null)
                {
                    _prefabs.RemoveAt(i);
                    i--;
                }
            }

            if (Application.isPlaying)
                InitializePrefabRange(0);
        }

        public override void AddObject(NetworkObject networkObject, bool checkForDuplicates = false)
        {
            AddObjects(new NetworkObject[] { networkObject }, checkForDuplicates);
        }

        public override void AddObjects(List<NetworkObject> networkObjects, bool checkForDuplicates = false)
        {
            AddObjects(networkObjects.ToArray(), checkForDuplicates);
        }
        public override void AddObjects(NetworkObject[] networkObjects, bool checkForDuplicates = false)
        {
            if (!checkForDuplicates)
            {
                _prefabs.AddRange(networkObjects);
            }
            else
            {
                foreach (NetworkObject nob in networkObjects)
                    AddUniqueNetworkObject(nob);
            }

            if (Application.isPlaying)
                InitializePrefabRange(0);
        }

        private void AddUniqueNetworkObject(NetworkObject nob)
        {
            if (!_prefabs.Contains(nob))
                _prefabs.Add(nob);
        }

        public override void InitializePrefabRange(int startIndex)
        {
            for (int i = startIndex; i < _prefabs.Count; i++)
            {
                if (_prefabs[i] == null)
                    continue;

                _prefabs[i].SetPrefabId((short)i);
            }
        }


        #region Unused.
        public override void AddObject(DualPrefab dualPrefab, bool checkForDuplicates = false)
        {
            Debug.LogError($"Dual prefabs are not supported with SinglePrefabObjects. Make a DualPrefabObjects asset instead.");
        }

        public override void AddObjects(List<DualPrefab> dualPrefab, bool checkForDuplicates = false)
        {
            Debug.LogError($"Dual prefabs are not supported with SinglePrefabObjects. Make a DualPrefabObjects asset instead.");
        }

        public override void AddObjects(DualPrefab[] dualPrefab, bool checkForDuplicates = false)
        {
            Debug.LogError($"Dual prefabs are not supported with SinglePrefabObjects. Make a DualPrefabObjects asset instead.");
        }
        #endregion
    }
}