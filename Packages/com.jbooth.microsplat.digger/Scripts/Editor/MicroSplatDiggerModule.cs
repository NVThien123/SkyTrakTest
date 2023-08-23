//////////////////////////////////////////////////////
// MicroSplat
// Copyright (c) Jason Booth
//////////////////////////////////////////////////////


using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Collections.Generic;
using System.Linq;

namespace JBooth.MicroSplat
{
#if __MICROSPLAT__
   [InitializeOnLoad]
   public class MicroSplatDiggerModule : FeatureDescriptor
   {
      const string sDefine = "__MICROSPLAT_DIGGER__";
      static MicroSplatDiggerModule ()
      {
         MicroSplatDefines.InitDefine (sDefine);
      }
      [PostProcessSceneAttribute (0)]
      public static void OnPostprocessScene ()
      {
         MicroSplatDefines.InitDefine (sDefine);
      }
      public override string ModuleName ()
      {
         return "Digger Integration";
      }

      public override string GetHelpPath ()
      {
         return "https://docs.google.com/document/d/1r9a4VL9_Ge6Hr8KlQ7yrmvVZG6GSiDeKPEX1hYZs1m0/edit?usp=sharing";
      }

      public enum DefineFeature
      {
         _OUTPUTDIGGER, // tells the compiler to make another shader for digger
         kNumFeatures,
      };

      TextAsset funcs;
      bool diggerEnabled;

      static Dictionary<DefineFeature, string> sFeatureNames = new Dictionary<DefineFeature, string> ();
      public static string GetFeatureName (DefineFeature feature)
      {
         string ret;
         if (sFeatureNames.TryGetValue (feature, out ret))
         {
            return ret;
         }
         string fn = System.Enum.GetName (typeof (DefineFeature), feature);
         sFeatureNames [feature] = fn;
         return fn;
      }

      public static bool HasFeature (string [] keywords, DefineFeature feature)
      {
         string f = GetFeatureName (feature);
         for (int i = 0; i < keywords.Length; ++i)
         {
            if (keywords [i] == f)
               return true;
         }
         return false;
      }

      public static bool HasFeature (string [] keywords, string f)
      {
         for (int i = 0; i < keywords.Length; ++i)
         {
            if (keywords [i] == f)
               return true;
         }
         return false;
      }

      public override string GetVersion ()
      {
         return "3.9";
      }

      static GUIContent CEnableDigger = new GUIContent ("Export Digger Shader", "Create a shader for digger to use?");
      bool alpha = false;
      bool triplanar = false;
      bool fixIt = false;

      public override void DrawFeatureGUI (MicroSplatKeywords keywords)
      {
         diggerEnabled = EditorGUILayout.Toggle (CEnableDigger, diggerEnabled);
         if (diggerEnabled)
         {
            // test for correct modules installed
            #if !__MICROSPLAT_TRIPLANAR__ || (!UNITY_2019_3_OR_NEWER && !__MICROSPLAT_ALPHAHOLE__)
            using (new GUILayout.VerticalScope (GUI.skin.box))
            {
               #if !__MICROSPLAT_TRIPLANAR__

               EditorGUILayout.HelpBox ("Digger is enabled, but triplanar module isn't installed. Digger geometry doesn't have UV coordinates, and needs the triplanar module to look correct", MessageType.Error);
               if (GUILayout.Button("Get Triplanar Module"))
               {
                  Application.OpenURL ("https://assetstore.unity.com/packages/tools/terrain/microsplat-triplanar-uvs-96777?pubref=25047");
               }
            
         
               #endif // !__MICROSPLAT_TRIPLANAR__
               #if !__MICROSPLAT_ALPHAHOLE__ && !UNITY_2019_3_OR_NEWER

               EditorGUILayout.HelpBox ("Digger is enabled, but terrain hole module isn't installed. Install Terrain Holes or upgrade to Unity 2019.3, which includes a terrain hole feature", MessageType.Error);
               if (GUILayout.Button("Get Terrain Hole Module"))
               {
                  Application.OpenURL ("https://assetstore.unity.com/packages/tools/terrain/microsplat-terrain-holes-97495?pubref=25047");
               }
         
               #endif // !__MICROSPLAT_ALPHAHOLE__ && !UNITY_2019_3_OR_NEWER
            }
            #else // !__MICROSPLAT_TRIPLANAR__ || (!UNITY_2019_3_OR_NEWER && !__MICROSPLAT_ALPHAHOLE__)

            // Test for correct setup of features
            
            if (!triplanar || !alpha)
            {
               using (new GUILayout.VerticalScope (GUI.skin.box))
               {
                  if (!triplanar && alpha)
                  {
                     EditorGUILayout.HelpBox ("Digger requires Triplanar Texturing turned on to look correct", MessageType.Error);
                  }
                  if (!triplanar && !alpha)
                  {
                     EditorGUILayout.HelpBox ("Digger requires Triplanar Texturing turned on and Alpha Hole set to ClipMap", MessageType.Error);
                  }
                  if (!alpha && triplanar)
                  {
                     EditorGUILayout.HelpBox ("Digger requires Alpha Hole set to ClipMap", MessageType.Error);
                  }
                  if (GUILayout.Button ("Fix It"))
                  {
                     fixIt = true;
                  }
                  EditorGUILayout.Space ();
               }
               
            }
            #endif // !__MICROSPLAT_TRIPLANAR__ || (!UNITY_2019_3_OR_NEWER && !__MICROSPLAT_ALPHAHOLE__)
         }



      }


      public override void DrawShaderGUI (MicroSplatShaderGUI shaderGUI, MicroSplatKeywords keywords, Material mat, MaterialEditor materialEditor, MaterialProperty [] props)
      {
         
      }

      public override MicroSplatShaderGUI.MicroSplatCompiler.AuxShader GetAuxShader ()
      {
         return new MicroSplatShaderGUI.MicroSplatCompiler.AuxShader ("_OUTPUTDIGGER", "_Digger");
      }

      public override void ModifyKeywordsForAuxShader (List<string> keywords)
      {
         if (keywords.Contains("_OUTPUTDIGGER"))
         {
            keywords.Remove ("_OUTPUTDIGGER");
            keywords.Remove ("_MICROTERRAIN");
            keywords.Remove ("_VSGRASSMAP");
            keywords.Remove ("_VSSHADOWMAP");
            keywords.Remove ("_PERPIXNORMAL");
            keywords.Add ("_MICRODIGGERMESH");

            keywords.Remove ("_ALPHATEST_ON");
            keywords.Remove ("_ALPHAHOLE");
            keywords.Remove ("_ALPHAHOLETEXTURE");
            keywords.Remove ("_TESSDISTANCE");
         }
      }

      public override void InitCompiler (string [] paths)
      {
         for (int i = 0; i < paths.Length; ++i)
         {
            string p = paths [i];
            if (p.EndsWith ("microsplat_digger_func.txt"))
            {
               funcs = AssetDatabase.LoadAssetAtPath<TextAsset> (p);
            }
         }
      }

      public override void WriteProperties (string [] features, System.Text.StringBuilder sb)
      {
         
      }

      public override void ComputeSampleCounts (string [] features, ref int arraySampleCount, ref int textureSampleCount, ref int maxSamples, ref int tessellationSamples, ref int depTexReadLevel)
      {

      }

      public override string [] Pack ()
      {
         List<string> features = new List<string> ();
         if (diggerEnabled)
         {
            features.Add (GetFeatureName (DefineFeature._OUTPUTDIGGER));
            if (fixIt)
            {
               fixIt = false;
               if (!triplanar)
               {
                  features.Add ("_TRIPLANAR");
               }
               if (!alpha)
               {
                  features.Add ("_ALPHAHOLETEXTURE");
               }
            }
         }
         return features.ToArray ();
      }

      public override void WritePerMaterialCBuffer (string [] features, System.Text.StringBuilder sb)
      {
         
      }

      public override void WriteFunctions(string [] features, System.Text.StringBuilder sb)
      {
         if (diggerEnabled)
         {
            sb.AppendLine (funcs.text);
         }
      }

      public override void Unpack (string [] keywords)
      {
         diggerEnabled = HasFeature (keywords, "_OUTPUTDIGGER") || HasFeature(keywords, "_MICRODIGGERMESH");
         if (diggerEnabled)
         {
            triplanar = HasFeature(keywords, "_TRIPLANAR");
#if UNITY_2019_3_OR_NEWER
            alpha = true;
#else
            alpha = HasFeature (keywords, "_ALPHAHOLETEXTURE");
#endif
         }
      }
   }
#endif

}
