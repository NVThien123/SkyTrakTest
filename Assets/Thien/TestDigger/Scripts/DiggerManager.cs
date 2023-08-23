using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Digger.Modules.Core.Sources;
using Digger.Modules.Runtime.Sources;
using UnityEngine;

public class DiggerManager : MonoBehaviour
{
    public TerrainLayers layers;

    private DiggerSystem[] _diggers;

    private void Awake()
    {
        _diggers = gameObject.GetComponentsInChildren<DiggerSystem>();
        _camera = Camera.main;
        
        diggerMasterRuntime = FindObjectOfType<DiggerMasterRuntime>();
        if (!diggerMasterRuntime) {
            enabled = false;
            Debug.LogWarning(
                "DiggerRuntimeUsageExample component requires DiggerMasterRuntime component to be setup in the scene. DiggerRuntimeUsageExample will be disabled.");
        }
        
        SetupTerrains();
    }

    private void SetupTerrains()
    {
        var terrains = gameObject.GetComponentsInChildren<Terrain>();
        foreach (var terrain in terrains)
        {
            terrain.terrainData.terrainLayers = layers.terrainLayers;
        }
    }

    #region Handle_Digger

    [Header("Modification parameters")]
    public BrushType brush = BrushType.Sphere;
    public ActionType action = ActionType.Dig;
    [Range(0, 7)] public int textureIndex;
    [Range(0.5f, 10f)] public float size = 4f;
    [Range(0f, 1f)] public float opacity = 0.5f;

    [Header("Persistence parameters (make sure persistence is enabled in Digger Master Runtime)")]
    public KeyCode keyToPersistData = KeyCode.P;

    public KeyCode keyToDeleteData = KeyCode.K;

    private DiggerMasterRuntime diggerMasterRuntime;
    private Camera _camera;
    private Ray _ray;

    private void Update()
    {
        if (Input.GetMouseButton(0)) 
        {
            _ray = _camera.ScreenPointToRay(Input.mousePosition);
            // Perform a raycast to find terrain surface and call Modify method of DiggerMasterRuntime to edit it
            if (Physics.Raycast(_ray, out var hit, 2000f)) 
            {
                diggerMasterRuntime.ModifyAsyncBuffured(hit.point, brush, action, textureIndex, opacity, size);
            }
        }
        
        if (Input.GetKeyDown(keyToPersistData)) {
            diggerMasterRuntime.PersistAll();
#if !UNITY_EDITOR
            Debug.Log("Persisted all modified chunks");
#endif
        } else if (Input.GetKeyDown(keyToDeleteData)) {
            diggerMasterRuntime.DeleteAllPersistedData();
#if !UNITY_EDITOR
            Debug.Log("Deleted all persisted data");
#endif
        }
    }

    #endregion
}
