using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Terrain Layers", menuName = "Variable Asset/Data/Terrain Layers", order = 912)]
public class TerrainLayers : ScriptableObject
{
    public TerrainLayer[] terrainLayers;
}