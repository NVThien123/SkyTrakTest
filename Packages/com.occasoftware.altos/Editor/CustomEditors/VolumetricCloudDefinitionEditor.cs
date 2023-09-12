using System;
using UnityEngine;
using UnityEditor;
using System.Reflection;
using OccaSoftware.Altos.Runtime;

namespace OccaSoftware.Altos.Editor
{
    [CustomEditor(typeof(CloudDefinition))]
    [CanEditMultipleObjects]
    public class VolumetricCloudDefinitionEditor : UnityEditor.Editor
    {
        private class EditorParams
        {
            public SerializedProperty pageSelection;
            public SerializedProperty lowAltitudeModelingState;
            public SerializedProperty lowAltitudeLightingState;
            public SerializedProperty lowAltitudeWeatherState;
            public SerializedProperty lowAltitudeBaseState;
            public SerializedProperty lowAltitudeDetail1State;
            public SerializedProperty lowAltitudeCurlState;

            public EditorParams(SerializedObject serializedObject)
            {
                SetFieldsByName(serializedObject, this);
            }
        }

        private class BasicParams
        {
            public SerializedProperty stepCount;
            public SerializedProperty blueNoise;
            public SerializedProperty sunColor;
            public SerializedProperty ambientExposure;
            public SerializedProperty cheapAmbientLighting;
            public SerializedProperty HGEccentricityForward;
            public SerializedProperty HGEccentricityBackward;
            public SerializedProperty HGBlend;
            public SerializedProperty HGStrength;

            public SerializedProperty celestialBodySelection;
            public SerializedProperty planetRadius;
            public SerializedProperty cloudLayerHeight;
            public SerializedProperty cloudLayerThickness;
            public SerializedProperty cloudFadeDistance;
            public SerializedProperty visibilityKM;

            public SerializedProperty renderScaleSelection;
            public SerializedProperty renderInSceneView;
            public SerializedProperty taaEnabled;
            public SerializedProperty taaBlendFactor;
            public SerializedProperty depthCullOptions;
            public SerializedProperty subpixelJitterEnabled;

            public SerializedProperty castShadowsEnabled;
            public SerializedProperty screenShadows;
            public SerializedProperty shadowStrength;
            public SerializedProperty shadowResolution;
            public SerializedProperty shadowStepCount;
            public SerializedProperty shadowDistance;

            public BasicParams(SerializedObject serializedObject)
            {
                SetFieldsByName(serializedObject, this);
            }
        }

        public class LowAltitudeParams
        {
            public SerializedProperty extinctionCoefficient;
            public SerializedProperty cloudiness;
            public SerializedProperty heightDensityInfluence;
            public SerializedProperty cloudinessDensityInfluence;
            public SerializedProperty cloudDensityCurve;
            public SerializedProperty curve;
            public SerializedProperty distantCoverageDepth;
            public SerializedProperty distantCoverageAmount;

            public SerializedProperty maxLightingDistance;
            public SerializedProperty multipleScatteringAmpGain;
            public SerializedProperty multipleScatteringDensityGain;
            public SerializedProperty multipleScatteringOctaves;

            public LowAltitudeParams(SerializedObject serializedObject)
            {
                SetFieldsByName(serializedObject, this);
            }
        }

        private class WeatherParams
        {
            public SerializedProperty weathermapTexture;
            public SerializedProperty weathermapVelocity;
            public SerializedProperty weathermapScale;
            public SerializedProperty weathermapType;
            public SerializedProperty weathermapGain;
            public SerializedProperty weathermapLacunarity;
            public SerializedProperty weathermapOctaves;

            public WeatherParams(SerializedObject serializedObject)
            {
                SetFieldsByName(serializedObject, this);
            }
        }

        private class BaseParams
        {
            public SerializedProperty baseTextureID;
            public SerializedProperty baseTextureQuality;
            public SerializedProperty baseTextureScale;
            public SerializedProperty baseTextureTimescale;

            public SerializedProperty baseFalloffSelection;
            public SerializedProperty baseTextureRInfluence;
            public SerializedProperty baseTextureGInfluence;
            public SerializedProperty baseTextureBInfluence;
            public SerializedProperty baseTextureAInfluence;

            public BaseParams(SerializedObject serializedObject)
            {
                SetFieldsByName(serializedObject, this);
            }
        }

        private class DetailParams
        {
            public SerializedProperty detail1TextureID;
            public SerializedProperty detail1TextureQuality;
            public SerializedProperty detail1TextureInfluence;
            public SerializedProperty detail1TextureScale;
            public SerializedProperty detail1TextureTimescale;

            public SerializedProperty detail1FalloffSelection;
            public SerializedProperty detail1TextureRInfluence;
            public SerializedProperty detail1TextureGInfluence;
            public SerializedProperty detail1TextureBInfluence;
            public SerializedProperty detail1TextureAInfluence;

            public SerializedProperty detail1TextureHeightRemap;

            public DetailParams(SerializedObject serializedObject)
            {
                SetFieldsByName(serializedObject, this);
            }
        }

        private class CurlParams
        {
            public SerializedProperty curlTexture;
            public SerializedProperty curlTextureInfluence;
            public SerializedProperty curlTextureScale;
            public SerializedProperty curlTextureTimescale;

            public CurlParams(SerializedObject serializedObject)
            {
                SetFieldsByName(serializedObject, this);
            }
        }

        private class HighAltitudeParams
        {
            public SerializedProperty highAltExtinctionCoefficient;
            public SerializedProperty highAltCloudiness;

            public SerializedProperty highAltTex1;
            public SerializedProperty highAltScale1;
            public SerializedProperty highAltTimescale1;

            public SerializedProperty highAltTex2;
            public SerializedProperty highAltScale2;
            public SerializedProperty highAltTimescale2;

            public SerializedProperty highAltTex3;
            public SerializedProperty highAltScale3;
            public SerializedProperty highAltTimescale3;

            public HighAltitudeParams(SerializedObject serializedObject)
            {
                SetFieldsByName(serializedObject, this);
            }
        }

        private static void SetFieldsByName<T>(SerializedObject serializedObject, T target)
        {
            foreach (FieldInfo field in typeof(T).GetFields())
            {
                field.SetValue(target, serializedObject.FindProperty(field.Name));
            }
        }

        public SerializedProperty curve_T;

        private EditorParams editorParams;
        private BasicParams basicParams;
        private LowAltitudeParams lowAltitudeParams;
        private WeatherParams weatherParams;
        private BaseParams baseParams;
        private DetailParams detailParams;
        private CurlParams curlParams;
        private HighAltitudeParams highAltitudeParams;

        private void OnEnable()
        {
            editorParams = new EditorParams(serializedObject);
            basicParams = new BasicParams(serializedObject);
            lowAltitudeParams = new LowAltitudeParams(serializedObject);
            weatherParams = new WeatherParams(serializedObject);
            baseParams = new BaseParams(serializedObject);
            detailParams = new DetailParams(serializedObject);
            curlParams = new CurlParams(serializedObject);
            highAltitudeParams = new HighAltitudeParams(serializedObject);

            curve_T = serializedObject.FindProperty("curve.m_Curve");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            Draw();
            serializedObject.ApplyModifiedProperties();
        }

        private void Draw()
        {
            DrawCloudDefinitionHeader();

            switch (editorParams.pageSelection.enumValueIndex)
            {
                case 0:
                    DrawVolumetricBasicSetup();
                    break;
                case 1:
                    HandleLowAltitudeDrawing();
                    break;
                case 2:
                    HandleHighAltitudeDrawing();
                    break;
                default:
                    break;
            }
        }

        void DrawCloudDefinitionHeader()
        {
            EditorGUILayout.BeginHorizontal();
            GUIStyle style = new GUIStyle(EditorStyles.toolbarButton);
            GUIStyle bold = new GUIStyle(EditorStyles.toolbarButton);
            style.fontStyle = FontStyle.Normal;
            bold.fontStyle = FontStyle.Bold;

            if (GUILayout.Button("Common", editorParams.pageSelection.enumValueIndex == 0 ? bold : style))
            {
                editorParams.pageSelection.enumValueIndex = 0;
            }
            if (GUILayout.Button("Low Altitude", editorParams.pageSelection.enumValueIndex == 1 ? bold : style))
            {
                editorParams.pageSelection.enumValueIndex = 1;
            }

            if (GUILayout.Button("High Altitude", editorParams.pageSelection.enumValueIndex == 2 ? bold : style))
            {
                editorParams.pageSelection.enumValueIndex = 2;
            }
            EditorGUILayout.EndHorizontal();
        }

        #region Draw Basic Setup
        void DrawVolumetricBasicSetup()
        {
            #region Basic Setup
            EditorHelpers.HandleIndentedGroup("Rendering", DrawBasicRendering);
            EditorHelpers.HandleIndentedGroup("Lighting", DrawBasicLighting);
            EditorHelpers.HandleIndentedGroup("Atmosphere", DrawBasicAtmosphere);
            EditorHelpers.HandleIndentedGroup("Shadows", DrawBasicShadows);
            #endregion
        }

        void DrawBasicRendering()
        {
            EditorGUILayout.IntSlider(basicParams.stepCount, 1, 128);
            EditorGUILayout.Slider(basicParams.blueNoise, 0f, 1f, new GUIContent("Noise"));

            EditorGUILayout.PropertyField(basicParams.renderScaleSelection, new GUIContent("Render Scale"));
            EditorGUILayout.PropertyField(basicParams.renderInSceneView);
            EditorGUILayout.PropertyField(basicParams.taaEnabled);
            EditorGUILayout.Slider(basicParams.taaBlendFactor, 0f, 1f);
            EditorGUILayout.PropertyField(basicParams.subpixelJitterEnabled);
            EditorGUILayout.PropertyField(basicParams.depthCullOptions, new GUIContent("Rendering Mode"));
        }

        void DrawBasicLighting()
        {
            basicParams.sunColor.colorValue = EditorGUILayout.ColorField(
                new GUIContent("Sun Color Mask", "This value is multiplied by the color of your main directional light."),
                basicParams.sunColor.colorValue,
                false,
                false,
                true
            );
            EditorGUILayout.PropertyField(basicParams.ambientExposure);
            EditorGUILayout.PropertyField(basicParams.cheapAmbientLighting);
            EditorGUILayout.Slider(basicParams.HGStrength, 0f, 1f);
            EditorGUILayout.Slider(basicParams.HGEccentricityForward, 0f, 0.99f);
            EditorGUILayout.Slider(basicParams.HGEccentricityBackward, -0.99f, 0f);
        }

        void DrawBasicAtmosphere()
        {
            EditorGUILayout.PropertyField(basicParams.celestialBodySelection, new GUIContent("Planet Radius"));
            if (GetEnum<CelestialBodySelection>(basicParams.celestialBodySelection) == CelestialBodySelection.Custom)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(
                    basicParams.planetRadius,
                    new GUIContent(
                        "Planet Radius (km)",
                        "Sets the size of the simulated planet. Larger values mean the cloud layer appears flatter. Smaller values mean the cloud layer is more obviously curved."
                    )
                );
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.PropertyField(
                basicParams.cloudLayerHeight,
                new GUIContent("Cloud Layer Altitude (km)", "Sets the floor of the cloud layer. Larger values mean the clouds start higher up.")
            );
            EditorGUILayout.Slider(
                basicParams.cloudLayerThickness,
                0.1f,
                4.0f,
                new GUIContent("Cloud Layer Thickness (km)", "Sets the size of the cloud layer. Larger values mean taller clouds.")
            );
            EditorGUILayout.PropertyField(
                basicParams.cloudFadeDistance,
                new GUIContent("Max Distance (km)", "Sets the maximum rendering distance for clouds. Larger values mean clouds render farther away.")
            );
            EditorGUILayout.PropertyField(
                basicParams.visibilityKM,
                new GUIContent(
                    "Visibility (km)",
                    "Sets the distance over which the clouds fade into the atmosphere. Larger values mean the clouds remain visible for longer."
                )
            );
        }

        void DrawBasicShadows()
        {
            EditorGUILayout.PropertyField(basicParams.castShadowsEnabled, new GUIContent("Cast Cloud Shadows"));
            if (basicParams.castShadowsEnabled.boolValue)
            {
                EditorGUILayout.PropertyField(
                    basicParams.screenShadows,
                    new GUIContent(
                        "Screen Shadows Enabled",
                        "When enabled, Altos applies cloud shadows for you as a post-process. This is easy, but it is not physically realistic: It will equally attenuate ambient, additional, and direct lighting. This only applies to depth-tested opaque geometry. When disabled, Altos allows you to write your own shadow sampler into your frag shader stage for objects in your scene for more realistic results. More complicated, but better results. See docs for details."
                    )
                );
                EditorGUILayout.PropertyField(basicParams.shadowResolution, new GUIContent("Resolution"));
                EditorGUILayout.PropertyField(basicParams.shadowStepCount, new GUIContent("Step Count"));
                EditorGUILayout.PropertyField(basicParams.shadowStrength, new GUIContent("Strength"));
                EditorGUILayout.PropertyField(
                    basicParams.shadowDistance,
                    new GUIContent("Max Distance (m)", "Radius around the camera in which cloud shadows will be rendered.")
                );
            }
        }
        #endregion

        #region Low Altitude
        void HandleLowAltitudeDrawing()
        {
            editorParams.lowAltitudeModelingState.boolValue = EditorHelpers.HandleFoldOutGroup(
                editorParams.lowAltitudeModelingState.boolValue,
                "Modeling",
                DrawLowAltitudeModeling
            );
            editorParams.lowAltitudeLightingState.boolValue = EditorHelpers.HandleFoldOutGroup(
                editorParams.lowAltitudeLightingState.boolValue,
                "Lighting",
                DrawLowAltitudeLighting
            );
            editorParams.lowAltitudeWeatherState.boolValue = EditorHelpers.HandleFoldOutGroup(
                editorParams.lowAltitudeWeatherState.boolValue,
                "Weather",
                DrawLowAltitudeWeather
            );
            editorParams.lowAltitudeBaseState.boolValue = EditorHelpers.HandleFoldOutGroup(
                editorParams.lowAltitudeBaseState.boolValue,
                "Base Clouds",
                DrawLowAltitudeBase
            );

            if (GetEnum<TextureIdentifier>(baseParams.baseTextureID) != TextureIdentifier.None)
            {
                editorParams.lowAltitudeDetail1State.boolValue = EditorHelpers.HandleFoldOutGroup(
                    editorParams.lowAltitudeDetail1State.boolValue,
                    "Cloud Detail",
                    DrawLowAltitudeDetail1
                );
                editorParams.lowAltitudeCurlState.boolValue = EditorHelpers.HandleFoldOutGroup(
                    editorParams.lowAltitudeCurlState.boolValue,
                    "Cloud Distortion",
                    DrawLowAltitudeDistortion
                );
            }
        }

        void DrawLowAltitudeModeling()
        {
            #region Modeling
            EditorGUILayout.PropertyField(lowAltitudeParams.extinctionCoefficient);
            EditorGUILayout.Slider(lowAltitudeParams.cloudiness, 0f, 1f);

            EditorGUILayout.Slider(lowAltitudeParams.cloudinessDensityInfluence, 0f, 1f);
            EditorGUILayout.Slider(lowAltitudeParams.heightDensityInfluence, 0f, 1f);
            EditorGUI.BeginChangeCheck();
            EditorHelpers.CurveProperty("Cloud Density Curve", curve_T);
            if (EditorGUI.EndChangeCheck())
            {
                var t = target as CloudDefinition;

                if (t == null)
                    return;

                t.curve.Release();
                t.curve.SetDirty();
            }
            #endregion

            #region Distant Coverage Configuration
            // Distant Coverage Configuration
            EditorGUILayout.LabelField("Distant Coverage", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(lowAltitudeParams.distantCoverageDepth, new GUIContent("Start Distance"));
            EditorGUILayout.Slider(lowAltitudeParams.distantCoverageAmount, 0f, 1f, new GUIContent("Cloudiness"));
            EditorGUI.indentLevel--;
            #endregion
        }

        void DrawLowAltitudeLighting()
        {
            EditorGUILayout.PropertyField(lowAltitudeParams.maxLightingDistance);
            EditorGUILayout.LabelField("Multiple Scattering", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.IntSlider(lowAltitudeParams.multipleScatteringOctaves, 1, 4, new GUIContent("Octaves"));
            if (lowAltitudeParams.multipleScatteringOctaves.intValue > 1)
            {
                EditorGUILayout.Slider(lowAltitudeParams.multipleScatteringAmpGain, 0f, 1f, new GUIContent("Amp Gain"));
                EditorGUILayout.Slider(lowAltitudeParams.multipleScatteringDensityGain, 0f, 1f, new GUIContent("Density Gain"));
            }
            EditorGUI.indentLevel--;
        }

        void DrawLowAltitudeWeather()
        {
            EditorGUILayout.PropertyField(weatherParams.weathermapType);
            if (weatherParams.weathermapType.enumValueIndex == (int)WeathermapType.Texture)
            {
                EditorGUILayout.PropertyField(weatherParams.weathermapTexture);
                EditorGUILayout.PropertyField(weatherParams.weathermapVelocity);
            }
            else
            {
                EditorGUILayout.LabelField("Type: Perlin");
                EditorGUILayout.PropertyField(weatherParams.weathermapOctaves, new GUIContent("Octaves"));
                if (weatherParams.weathermapOctaves.intValue > 1)
                {
                    EditorGUILayout.PropertyField(weatherParams.weathermapGain, new GUIContent("Gain"));
                    EditorGUILayout.PropertyField(weatherParams.weathermapLacunarity, new GUIContent("Frequency"));
                }

                EditorGUILayout.PropertyField(weatherParams.weathermapScale, new GUIContent("Scale"));
                EditorGUILayout.PropertyField(weatherParams.weathermapVelocity, new GUIContent("Velocity"));
            }
        }

        #region Draw Base
        void DrawLowAltitudeBase()
        {
            EditorGUILayout.PropertyField(baseParams.baseTextureID, new GUIContent("Type"));

            if (GetEnum<TextureIdentifier>(baseParams.baseTextureID) != TextureIdentifier.None)
            {
                EditorGUILayout.PropertyField(baseParams.baseTextureQuality, new GUIContent("Quality"));
                EditorGUILayout.Space(5);

                //EditorGUILayout.LabelField("Scale", EditorStyles.boldLabel);
                float s = EditorGUILayout.FloatField("Scale", baseParams.baseTextureScale.vector3Value.x);
                baseParams.baseTextureScale.vector3Value = new Vector3(s, s, s);
                EditorGUILayout.PropertyField(baseParams.baseTextureTimescale, new GUIContent("Velocity"));
                EditorGUILayout.Space(5);
            }
        }
        #endregion


        #region Draw Detail
        private void DrawLowAltitudeDetail1()
        {
            DetailData detailData = new DetailData
            {
                texture = detailParams.detail1TextureID,
                quality = detailParams.detail1TextureQuality,
                influence = detailParams.detail1TextureInfluence,
                heightRemap = detailParams.detail1TextureHeightRemap,
                scale = detailParams.detail1TextureScale,
                timescale = detailParams.detail1TextureTimescale,
                falloffSelection = detailParams.detail1FalloffSelection,
            };

            DrawDetail(detailData);
        }

        private void DrawDetail(DetailData detailData)
        {
            EditorGUILayout.PropertyField(detailData.texture, new GUIContent("Type"));

            if (GetEnum<TextureIdentifier>(detailData.texture) != TextureIdentifier.None)
            {
                EditorGUILayout.PropertyField(detailData.quality, new GUIContent("Quality"));
                EditorGUILayout.Space(5);

                //EditorGUILayout.LabelField("Strength", EditorStyles.boldLabel);
                EditorGUILayout.Slider(detailData.influence, 0f, 1f, new GUIContent("Intensity"));
                EditorGUILayout.PropertyField(detailData.heightRemap, new GUIContent("Height Mapping"));
                EditorGUILayout.Space(5);

                //EditorGUILayout.LabelField("Scale", EditorStyles.boldLabel);
                float s = EditorGUILayout.FloatField("Scale", detailData.scale.vector3Value.x);
                detailData.scale.vector3Value = new Vector3(s, s, s);
                EditorGUILayout.PropertyField(detailData.timescale, new GUIContent("Velocity"));
                EditorGUILayout.Space(5);
            }
        }

        private struct DetailData
        {
            public SerializedProperty texture;
            public SerializedProperty quality;
            public SerializedProperty influence;
            public SerializedProperty heightRemap;
            public SerializedProperty scale;
            public SerializedProperty timescale;
            public SerializedProperty falloffSelection;
        }
        #endregion


        #region Draw Distortion
        private void DrawLowAltitudeDistortion()
        {
            EditorGUILayout.PropertyField(curlParams.curlTexture, new GUIContent("Texture"));
            if (curlParams.curlTexture.objectReferenceValue != null)
            {
                EditorGUILayout.PropertyField(curlParams.curlTextureInfluence, new GUIContent("Intensity"));
                EditorGUILayout.PropertyField(curlParams.curlTextureScale, new GUIContent("Scale"));
                EditorGUILayout.PropertyField(curlParams.curlTextureTimescale, new GUIContent("Speed"));
            }
        }
        #endregion
        #endregion

        void HandleHighAltitudeDrawing()
        {
            bool tex1State = highAltitudeParams.highAltTex1.objectReferenceValue != null ? true : false;
            bool tex2State = highAltitudeParams.highAltTex2.objectReferenceValue != null ? true : false;
            bool tex3State = highAltitudeParams.highAltTex3.objectReferenceValue != null ? true : false;
            bool aggTexState = tex1State || tex2State || tex3State ? true : false;

            if (aggTexState)
            {
                EditorGUILayout.LabelField("Modeling", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(highAltitudeParams.highAltExtinctionCoefficient, new GUIContent("Extinction Coefficient"));
                EditorGUILayout.Slider(highAltitudeParams.highAltCloudiness, 0f, 1f, new GUIContent("Cloudiness"));
                EditorGUI.indentLevel--;
            }

            if (aggTexState)
            {
                EditorGUILayout.Space(10f);
                EditorGUILayout.LabelField("Textures", EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
            }

            EditorGUILayout.LabelField("Weathermap", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(highAltitudeParams.highAltTex1, new GUIContent("Texture"));
            if (tex1State)
            {
                EditorGUILayout.PropertyField(highAltitudeParams.highAltScale1, new GUIContent("Scale"));
                EditorGUILayout.PropertyField(highAltitudeParams.highAltTimescale1, new GUIContent("Timescale"));
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Cloud Texture 1", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(highAltitudeParams.highAltTex2, new GUIContent("Texture"));
            if (tex2State)
            {
                EditorGUILayout.PropertyField(highAltitudeParams.highAltScale2, new GUIContent("Scale"));
                EditorGUILayout.PropertyField(highAltitudeParams.highAltTimescale2, new GUIContent("Timescale"));
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Cloud Texture 2", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(highAltitudeParams.highAltTex3, new GUIContent("Texture"));
            if (tex3State)
            {
                EditorGUILayout.PropertyField(highAltitudeParams.highAltScale3, new GUIContent("Scale"));
                EditorGUILayout.PropertyField(highAltitudeParams.highAltTimescale3, new GUIContent("Timescale"));
            }
            EditorGUI.indentLevel--;

            if (aggTexState)
                EditorGUI.indentLevel--;
        }

        T GetEnum<T>(SerializedProperty property)
        {
            return (T)Enum.ToObject(typeof(T), property.enumValueIndex);
        }
    }

    internal static class EditorHelpers
    {
        public static bool HandleFoldOutGroup(bool state, string header, Action controls)
        {
            state = EditorGUILayout.BeginFoldoutHeaderGroup(state, header);

            if (state)
            {
                EditorGUI.indentLevel++;
                controls();
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(10);
            }

            EditorGUILayout.EndFoldoutHeaderGroup();

            return state;
        }

        public static void HandleIndentedGroup(string header, Action controls)
        {
            EditorGUILayout.LabelField(header, EditorStyles.boldLabel);

            EditorGUI.indentLevel++;
            controls();
            EditorGUI.indentLevel--;

            EditorGUILayout.Space(10);
        }

        public static void CurveProperty(string label, SerializedProperty curve)
        {
            curve.animationCurveValue = EditorGUILayout.CurveField(label, curve.animationCurveValue, Color.white, new Rect(0, 0, 1, 1));
        }
    }
}
