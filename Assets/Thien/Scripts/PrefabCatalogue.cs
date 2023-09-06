using System;
using System.Collections.Generic;
using UnityEngine;

namespace Thien.Scripts
{
    [CreateAssetMenu(fileName = "Prefab Catalogue", menuName = "Variable Asset/Data/Prefab Catalogue", order = 912)]
    public class PrefabCatalogue : ScriptableObject
    {
        public List<PrefabCatalogItem> prefabCatalogItems;

        private Dictionary<int, PrefabCatalogItem> _dict = new Dictionary<int, PrefabCatalogItem>();

        public void RuntimeInit()
        {
            _dict.Clear();

            var idx = 0;
            foreach (var item in prefabCatalogItems)
            {
                if (item.prefabs.Count == 0) continue;

                _dict.Add(idx++, item);
            }
        }

        public List<string> GetCatalogName()
        {
            var result = new List<string>();

            foreach (var item in _dict)
            {
                result.Add(item.Value.typeName);
            }

            return result;
        }

        public List<GameObject> GetListPrefab(int index)
        {
            return _dict.ContainsKey(index) ? _dict[index].prefabs : new List<GameObject>();
        }
    }

    [Serializable]
    public class PrefabCatalogItem
    {
        public string typeName;
        public List<GameObject> prefabs;
    }
}
