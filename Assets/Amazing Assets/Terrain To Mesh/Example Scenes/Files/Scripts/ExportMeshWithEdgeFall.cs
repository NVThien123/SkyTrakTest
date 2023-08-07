﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace AmazingAssets.TerrainToMesh.Example
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class ExportMeshWithEdgeFall : MonoBehaviour
    {
        public TerrainData terrainData;

        public int vertexCountHorizontal = 100;
        public int vertexCountVertical = 100;

        [Space(10)]
        public EdgeFall edgeFall = new EdgeFall(0, true);
        public Texture2D edgeFallTexture;


        void Start()
        {
            if (terrainData == null)
                return;


            //1. Export mesh with edge fall/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            Mesh terrainMesh = terrainData.TerrainToMesh().ExportMesh(vertexCountHorizontal, vertexCountVertical, TerrainToMesh.Normal.CalculateFromMesh, edgeFall);

            GetComponent<MeshFilter>().sharedMesh = terrainMesh;




            //2. Create materials////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            Material meshMaterial = new Material(Shader.Find("Standard"));      //Material for the main mesh 

            Material edgeFallMaterial = new Material(Shader.Find("Standard"));  //Material for the edge fall (saved in sub-mesh)
            edgeFallMaterial.SetTexture("_MainTex", edgeFallTexture);           //Prop name is defined inside shader


            GetComponent<Renderer>().sharedMaterials = new Material[] { meshMaterial, edgeFallMaterial };
        }
    }
}
