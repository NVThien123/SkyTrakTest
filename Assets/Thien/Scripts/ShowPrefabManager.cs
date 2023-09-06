using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Thien.Scripts
{
    public class ShowPrefabManager : MonoBehaviour
    {
        public PrefabCatalogue prefabCatalogue;
        
        [SerializeField] private TMP_Dropdown catalogDropdown;
        [SerializeField] private TMP_Dropdown prefabDropdown;

        [SerializeField] private Camera mainCamera;
        
        private Vector3 _originPosition;
        private Quaternion _originRotation;
    
        // Start is called before the first frame update
        void Start()
        {
            prefabCatalogue.RuntimeInit();
            
            var trans = mainCamera.transform;
            _originPosition = trans.position;
            _originRotation = trans.rotation;
            
            prefabDropdown.options = new List<TMP_Dropdown.OptionData> { new TMP_Dropdown.OptionData("None") };

            prefabDropdown.onValueChanged.RemoveListener(HandleSelectPrefab);
            prefabDropdown.onValueChanged.AddListener(HandleSelectPrefab);
            
            var catalogName = prefabCatalogue.GetCatalogName();
            var options = catalogName.Select(t => new TMP_Dropdown.OptionData(t)).ToList();
            
            catalogDropdown.options = options;
            
            catalogDropdown.onValueChanged.RemoveListener(HandleCatalogChange);
            catalogDropdown.onValueChanged.AddListener(HandleCatalogChange);
            
            catalogDropdown.value = 0;
            HandleCatalogChange(catalogDropdown.value);
        }

        private void HandleCatalogChange(int index)
        {
            _listPrefab = prefabCatalogue.GetListPrefab(index);
            var options = _listPrefab.Select(t => new TMP_Dropdown.OptionData(t.name)).ToList();
            options.Insert(0, new TMP_Dropdown.OptionData("None"));

            prefabDropdown.options = options;
            
            prefabDropdown.value = 0;
            HandleSelectPrefab(prefabDropdown.value);
        }

        private List<GameObject> _listPrefab;
        private GameObject _prefab;
        private void HandleSelectPrefab(int index)
        {
            ResetView();
            
            index--;
            if (index < 0 || index >= _listPrefab.Count) return;

            _prefab = Instantiate(_listPrefab[index]);
        }

        private void ResetView()
        {
            if (_prefab != null) Destroy(_prefab.gameObject);
            
            var trans = mainCamera.transform;
            trans.position = _originPosition;
            trans.rotation = _originRotation;
        }
    }
}