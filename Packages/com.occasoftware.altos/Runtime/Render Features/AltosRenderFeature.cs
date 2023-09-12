using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OccaSoftware.Altos.Runtime
{
    /// <summary>
    /// Renders the Altos Sky, Sky Objects, Stars, Clouds, and Cloud Shadows.
    /// </summary>
    internal sealed class AltosRenderFeature : ScriptableRendererFeature
    {
        private class StarRenderPass
        {
            public void Dispose()
            {
                CoreUtils.Destroy(starMaterial);
                starMaterial = null;
                Cleanup();
            }

            private Material starMaterial;

            private Material GetStarMaterial()
            {
                if (starMaterial == null)
                {
                    starMaterial = CoreUtils.CreateEngineMaterial(skyDirector.data.shaders.starShader);
                }

                return starMaterial;
            }

            private Mesh mesh = null;
            private Mesh Mesh
            {
                get
                {
                    if (mesh == null)
                        mesh = Helpers.CreateQuad();

                    return mesh;
                }
            }

            private int initialSeed = 1;

            private ComputeBuffer meshPropertiesBuffer = null;

            private ComputeBuffer argsBuffer = null;

            private Texture2D starTexture;

            private Texture2D white = null;
            private Texture2D White
            {
                get
                {
                    if (white == null)
                    {
                        white = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                        white.SetPixel(0, 0, Color.white);
                        white.Apply();
                    }

                    return white;
                }
            }

            private bool initialized = false;

            private struct MeshProperties
            {
                public Matrix4x4 mat;
                public Vector3 color;
                public float brightness;
                public float id;

                public static int Size()
                {
                    return sizeof(float) * 4 * 4
                        + // matrix
                        sizeof(float) * 3
                        + // color
                        sizeof(float)
                        + // brightness
                        sizeof(float); // id
                }
            }

            private void Init()
            {
                Cleanup();
                InitializeBuffers();
            }

            private void Cleanup()
            {
                argsBuffer?.Release();
                argsBuffer = null;
                meshPropertiesBuffer?.Release();
                meshPropertiesBuffer = null;
            }

            public void Draw(ref CommandBuffer cmd, SkyDefinition skyboxDefinition)
            {
                if (!initialized || skyDirector.starDefinition.IsDirty())
                {
                    Init();
                    initialized = true;
                }

                if (Mesh == null || GetStarMaterial() == null || argsBuffer == null || meshPropertiesBuffer == null)
                {
                    return;
                }

                if (skyDirector.starDefinition.positionStatic)
                {
                    GetStarMaterial().SetFloat(ShaderParams._EarthTime, 0);
                }
                else
                {
                    float currentTime = 0f;
                    if (skyboxDefinition != null)
                        currentTime = skyboxDefinition.CurrentTime;

                    GetStarMaterial().SetFloat(ShaderParams._EarthTime, currentTime);
                }

                GetStarMaterial().SetFloat(ShaderParams._Brightness, GetStarBrightness());
                GetStarMaterial().SetFloat(ShaderParams._FlickerFrequency, skyDirector.starDefinition.flickerFrequency);
                GetStarMaterial().SetFloat(ShaderParams._FlickerStrength, skyDirector.starDefinition.flickerStrength);
                GetStarMaterial().SetFloat(ShaderParams._Inclination, -skyDirector.starDefinition.inclination);
                GetStarMaterial().SetColor(ShaderParams._Color, skyDirector.starDefinition.color);
                starTexture = skyDirector.starDefinition.texture == null ? White : skyDirector.starDefinition.texture;
                GetStarMaterial().SetTexture(ShaderParams._MainTex, starTexture);
                GetStarMaterial().SetBuffer(ShaderParams._Properties, meshPropertiesBuffer);
                cmd.DrawMeshInstancedIndirect(Mesh, 0, GetStarMaterial(), -1, argsBuffer);
            }

            private float GetStarBrightness()
            {
                float t = Vector3.Dot(Vector3.down, skyDirector.Sun.GetDirection());
                t = Mathf.Clamp01(t);
                t = 1.0f - t;
                t *= t;
                t = 1.0f - t;
                float brightness = Mathf.Lerp(skyDirector.starDefinition.dayBrightness, skyDirector.starDefinition.brightness, t);
                return brightness;
            }

            private static class ShaderParams
            {
                public static int _EarthTime = Shader.PropertyToID("_EarthTime");
                public static int _Brightness = Shader.PropertyToID("_Brightness");
                public static int _FlickerFrequency = Shader.PropertyToID("_FlickerFrequency");
                public static int _FlickerStrength = Shader.PropertyToID("_FlickerStrength");
                public static int _MainTex = Shader.PropertyToID("_MainTex");
                public static int _Properties = Shader.PropertyToID("_Properties");
                public static int _Inclination = Shader.PropertyToID("_Inclination");
                public static int _Color = Shader.PropertyToID("_Color");
            }

            private void InitializeBuffers()
            {
                if (skyDirector.starDefinition == null)
                    return;

                uint[] args = new uint[5] { 0, 0, 0, 0, 0 };
                args[0] = Mesh.GetIndexCount(0);
                args[1] = (uint)skyDirector.starDefinition.count;
                argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments, ComputeBufferMode.Immutable);
                argsBuffer.SetData(args);

                // Initialize buffer with the given population.
                MeshProperties[] meshPropertiesArray = new MeshProperties[skyDirector.starDefinition.count];
                Random.InitState(initialSeed);
                for (int i = 0; i < skyDirector.starDefinition.count; i++)
                {
                    MeshProperties meshProperties = new MeshProperties();
                    Vector3 position = Random.onUnitSphere * 100f;
                    Quaternion rotation = Quaternion.LookRotation(Vector3.zero - position, Random.onUnitSphere);
                    Vector3 scale = Vector3.one * Random.Range(1f, 2f) * 0.1f * skyDirector.starDefinition.size;

                    meshProperties.mat = Matrix4x4.TRS(position, rotation, scale);

                    if (skyDirector.starDefinition.automaticColor)
                    {
                        float temperature = Helpers.GetStarTemperature(Random.Range(0f, 1f));
                        meshProperties.color = Helpers.GetBlackbodyColor(temperature);
                    }
                    else
                    {
                        meshProperties.color = new Vector3(1, 1, 1);
                    }

                    if (skyDirector.starDefinition.automaticBrightness)
                    {
                        meshProperties.brightness = Helpers.GetStarBrightness(Random.Range(0f, 1f));
                    }
                    else
                    {
                        meshProperties.brightness = 1f;
                    }

                    meshProperties.id = Random.Range(0f, 1f);
                    meshPropertiesArray[i] = meshProperties;
                }
                meshPropertiesBuffer = new ComputeBuffer(
                    skyDirector.starDefinition.count,
                    MeshProperties.Size(),
                    ComputeBufferType.Structured,
                    ComputeBufferMode.Immutable
                );
                meshPropertiesBuffer.SetData(meshPropertiesArray);
            }
        }

        private class SkyRenderPass : ScriptableRenderPass
        {
            private const string profilerTag = "Altos: Render Sky";

            [System.NonSerialized]
            private StarRenderPass stars = null;

            private RTHandle skyTarget;

            private const string skyTargetId = "_SkyTexture";

            private Material atmosphereMaterial;
            private Material backgroundMaterial;

            public void Dispose()
            {
                CoreUtils.Destroy(atmosphereMaterial);
                CoreUtils.Destroy(backgroundMaterial);
                atmosphereMaterial = null;
                backgroundMaterial = null;
                stars?.Dispose();
            }

            public SkyRenderPass()
            {
                skyTarget = RTHandles.Alloc(Shader.PropertyToID(skyTargetId), name: skyTargetId);
            }

            public void Setup()
            {
                if (atmosphereMaterial == null)
                    atmosphereMaterial = CoreUtils.CreateEngineMaterial(skyDirector.data?.shaders.atmosphereShader);
                if (backgroundMaterial == null)
                    backgroundMaterial = CoreUtils.CreateEngineMaterial(skyDirector.data?.shaders.backgroundShader);
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                RenderTextureDescriptor skyTargetDescriptor = cameraTextureDescriptor;
                skyTargetDescriptor.msaaSamples = 1;
                skyTargetDescriptor.depthBufferBits = 0;
                skyTargetDescriptor.width = (int)(skyTargetDescriptor.width * 0.125f);
                skyTargetDescriptor.width = Mathf.Max(skyTargetDescriptor.width, 1);
                skyTargetDescriptor.height = (int)(skyTargetDescriptor.height * 0.125f);
                skyTargetDescriptor.width = Mathf.Max(skyTargetDescriptor.height, 1);

                RenderingUtils.ReAllocateIfNeeded(ref skyTarget, skyTargetDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: skyTargetId);

                if (skyDirector.starDefinition != null)
                {
                    if (stars == null)
                        stars = new StarRenderPass();
                }
            }

            internal static class SkyShaderParams
            {
                public static int _Direction = Shader.PropertyToID("_Direction");
                public static int _Color = Shader.PropertyToID("_Color");
                public static int _Falloff = Shader.PropertyToID("_Falloff");
                public static int _Count = Shader.PropertyToID("_SkyObjectCount");
            }

            private static int _MAX_SKY_OBJECT_COUNT = 8;
            Vector4[] directions = new Vector4[_MAX_SKY_OBJECT_COUNT];
            Vector4[] colors = new Vector4[_MAX_SKY_OBJECT_COUNT];
            float[] falloffs = new float[_MAX_SKY_OBJECT_COUNT];
            SkyObject skyObject = null;

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                Profiler.BeginSample(profilerTag);
                CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
                RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;

                cmd.SetRenderTarget(source);

                #region Draw Background
                Matrix4x4 m = Matrix4x4.identity;
                m.SetTRS(
                    renderingData.cameraData.worldSpaceCameraPos,
                    Quaternion.identity,
                    Vector3.one * renderingData.cameraData.camera.farClipPlane
                );
                cmd.DrawMesh(skyDirector.data.meshes.skyboxMesh, m, backgroundMaterial, 0);
                #endregion


                #region Draw Stars
                if (skyDirector.starDefinition != null)
                {
                    stars.Draw(ref cmd, skyDirector.skyDefinition);
                }

                #endregion


                #region Draw Sun and Moon
                int skyObjectCount = Mathf.Min(skyDirector.SkyObjects.Count, _MAX_SKY_OBJECT_COUNT);
                for (int i = 0; i < skyObjectCount; i++)
                {
                    skyObject = skyDirector.SkyObjects[i];

                    m.SetTRS(
                        skyObject.positionRelative + renderingData.cameraData.worldSpaceCameraPos,
                        skyObject.GetRotation(),
                        Vector3.one * skyObject.CalculateSize()
                    );

                    cmd.DrawMesh(skyObject.Quad, m, skyObject.GetMaterial());
                    directions[i] = skyObject.GetDirection();
                    colors[i] = skyObject.GetColor();
                    falloffs[i] = skyObject.GetFalloff();
                    skyObject = null;
                }

                cmd.SetGlobalVectorArray(SkyShaderParams._Direction, directions);
                cmd.SetGlobalVectorArray(SkyShaderParams._Color, colors);
                cmd.SetGlobalFloatArray(SkyShaderParams._Falloff, falloffs);
                cmd.SetGlobalInteger(SkyShaderParams._Count, skyObjectCount);
                #endregion


                #region Draw Sky
                if (skyDirector.skyDefinition != null)
                {
                    m.SetTRS(
                        renderingData.cameraData.camera.transform.position,
                        Quaternion.identity,
                        Vector3.one * renderingData.cameraData.camera.farClipPlane
                    );

                    cmd.SetGlobalColor(ShaderParams._HorizonColor, skyDirector.skyDefinition.SkyColors.equatorColor);
                    cmd.SetGlobalColor(ShaderParams._ZenithColor, skyDirector.skyDefinition.SkyColors.skyColor);
                    cmd.DrawMesh(skyDirector.data.meshes.skyboxMesh, m, atmosphereMaterial, 0);

                    // Draw Sky Target

                    cmd.SetRenderTarget(skyTarget);
                    cmd.ClearRenderTarget(true, true, Color.black);

                    cmd.SetGlobalInteger(SkyShaderParams._Count, 0);
                    cmd.DrawMesh(skyDirector.data.meshes.skyboxMesh, m, atmosphereMaterial, 0);
                    cmd.SetGlobalInt(ShaderParams._HasSkyTexture, 1);
                    cmd.SetGlobalTexture("_SkyTexture", skyTarget);

                    cmd.SetRenderTarget(source, renderingData.cameraData.renderer.cameraDepthTargetHandle);
                }
                #endregion


                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                CommandBufferPool.Release(cmd);
                Profiler.EndSample();
            }

            private static class ShaderParams
            {
                public static int _MainTex = Shader.PropertyToID("_MainTex");
                public static int _Color = Shader.PropertyToID("_Color");
                public static int _SunFalloff = Shader.PropertyToID("_SunFalloff");
                public static int _SunColor = Shader.PropertyToID("_SunColor");
                public static int _SunForward = Shader.PropertyToID("_SunForward");
                public static int _HorizonColor = Shader.PropertyToID("_HorizonColor");
                public static int _ZenithColor = Shader.PropertyToID("_ZenithColor");
                public static int _HasSkyTexture = Shader.PropertyToID("_HasSkyTexture");
            }
        }

        private static void AssignDefaultDescriptorSettings(
            ref RenderTextureDescriptor desc,
            RenderTextureFormat format = RenderTextureFormat.DefaultHDR
        )
        {
            desc.msaaSamples = 1;
            desc.depthBufferBits = 0;
            desc.width = Mathf.Max(1, desc.width);
            desc.height = Mathf.Max(1, desc.height);
            desc.useDynamicScale = false;
            desc.colorFormat = format;
        }

        private class VolumetricCloudsRenderPass : ScriptableRenderPass
        {
            #region RT Handles
            private RTHandle cloudTarget;
            private RTHandle temporalTarget;
            private RTHandle upscaleHalfRes;
            private RTHandle upscaleQuarterRes;
            private RTHandle mergeTarget;
            private RTHandle depthTex;
            #endregion

            #region Input vars
            private const string profilerTag = "Altos: Render Clouds";
            #endregion

            #region Shader Variable References
            private const string mergePassInputTextureShaderReference = "_MERGE_PASS_INPUT_TEX";
            private const string colorHistoryId = "_PREVIOUS_TAA_CLOUD_RESULTS";
            private const string depthId = "_DitheredDepthTex";
            #endregion

            #region Texture Ids
            private const string cloudId = "_CloudRenderPass";
            private const string upscaleHalfId = "_CloudUpscaleHalfResTarget";
            private const string upscaleQuarterId = "_CloudUpscaleQuarterResTarget";
            private const string taaId = "_CloudTemporalIntegration";
            private const string mergeId = "_CloudSceneMergeTarget";
            #endregion

            #region Materials
            private Material cloudTaa;
            private Material merge;
            private Material upscale;
            private Material ditherDepth;
            #endregion

            // RT Desc.
            private RenderTextureDescriptor cloudRenderDescriptor;

            // TAA Class
            private TemporalAA taa;

            public VolumetricCloudsRenderPass()
            {
                // Create TAA handler
                taa = new TemporalAA();

                // Setup RT Handles
                cloudTarget = RTHandles.Alloc(Shader.PropertyToID(cloudId), name: cloudId);
                upscaleHalfRes = RTHandles.Alloc(Shader.PropertyToID(upscaleHalfId), name: upscaleHalfId);
                upscaleQuarterRes = RTHandles.Alloc(Shader.PropertyToID(upscaleQuarterId), name: upscaleQuarterId);
                temporalTarget = RTHandles.Alloc(Shader.PropertyToID(taaId), name: taaId);
                mergeTarget = RTHandles.Alloc(Shader.PropertyToID(mergeId), name: mergeId);
                depthTex = RTHandles.Alloc(Shader.PropertyToID(depthId), name: depthId);
            }

            public void Dispose()
            {
                CoreUtils.Destroy(merge);
                CoreUtils.Destroy(cloudTaa);
                CoreUtils.Destroy(upscale);
                CoreUtils.Destroy(ditherDepth);
                merge = null;
                cloudTaa = null;
                upscale = null;
                ditherDepth = null;

                if (taa != null)
                {
                    taa.Dispose();
                }
            }

            public void Setup()
            {
                // Setup Materials
                if (merge == null)
                    merge = CoreUtils.CreateEngineMaterial(skyDirector.data.shaders.mergeClouds);
                if (cloudTaa == null)
                    cloudTaa = CoreUtils.CreateEngineMaterial(skyDirector.data.shaders.temporalIntegration);
                if (upscale == null)
                    upscale = CoreUtils.CreateEngineMaterial(skyDirector.data.shaders.upscaleClouds);
                if (ditherDepth == null)
                    ditherDepth = CoreUtils.CreateEngineMaterial(skyDirector.data.shaders.ditherDepth);
            }

            private static class TimeManager
            {
                private static float managedTime = 0;
                private static int frameCount = 0;

                public static float ManagedTime
                {
                    get => managedTime;
                }

                public static int FrameCount
                {
                    get => frameCount;
                }

                public static void Update()
                {
                    float unityRealtimeSinceStartup = Time.realtimeSinceStartup;
                    int unityFrameCount = Time.frameCount;

                    bool newFrame;
                    if (Application.isPlaying)
                    {
                        newFrame = frameCount != unityFrameCount;
                        frameCount = unityFrameCount;
                    }
                    else
                    {
                        newFrame = (unityRealtimeSinceStartup - managedTime) > 0.0166f;
                        if (newFrame)
                            frameCount++;
                    }

                    if (newFrame)
                    {
                        managedTime = unityRealtimeSinceStartup;
                    }
                }
            }

            private class TemporalAA
            {
                public TemporalAA()
                {
                    // Constructor
                }

                private List<Camera> removeTargets = new List<Camera>();

                private Dictionary<Camera, TAACameraData> temporalData = new Dictionary<Camera, TAACameraData>();
                public Dictionary<Camera, TAACameraData> TemporalData
                {
                    get => temporalData;
                }

                public void Cleanup()
                {
                    CleanupDictionary();
                }

                public void Dispose()
                {
                    foreach (KeyValuePair<Camera, TAACameraData> data in temporalData)
                    {
                        data.Value.Dispose();
                    }
                }

                internal class TAACameraData
                {
                    private int lastFrameUsed;
                    private RenderTexture colorTexture;
                    private string cameraName;
                    private Matrix4x4 prevViewProj;

                    public void Dispose()
                    {
                        colorTexture.Release();
                    }

                    public TAACameraData(int lastFrameUsed, RenderTexture colorTexture, string cameraName)
                    {
                        LastFrameUsed = lastFrameUsed;
                        ColorTexture = colorTexture;
                        CameraName = cameraName;
                        prevViewProj = Matrix4x4.identity;
                    }

                    public int LastFrameUsed
                    {
                        get => lastFrameUsed;
                        set => lastFrameUsed = value;
                    }

                    public RenderTexture ColorTexture
                    {
                        get => colorTexture;
                        set => colorTexture = value;
                    }

                    public string CameraName
                    {
                        get => cameraName;
                        set => cameraName = value;
                    }

                    public Matrix4x4 PrevViewProj
                    {
                        get => prevViewProj;
                        set => prevViewProj = value;
                    }
                }

                public bool IsTemporalDataValid(Camera camera, RenderTextureDescriptor descriptor)
                {
                    if (temporalData.TryGetValue(camera, out TAACameraData cameraData))
                    {
                        bool isColorTexValid = IsRenderTextureValid(descriptor, cameraData.ColorTexture);

                        if (isColorTexValid)
                            return true;
                    }

                    return false;

                    bool IsRenderTextureValid(RenderTextureDescriptor desc, RenderTexture rt)
                    {
                        if (rt == null)
                        {
                            return false;
                        }

                        bool rtWrongSize = (rt.width != desc.width || rt.height != desc.height) ? true : false;
                        if (rtWrongSize)
                        {
                            return false;
                        }

                        return true;
                    }
                }

                public void SetupTemporalData(Camera camera, RenderTextureDescriptor descriptor)
                {
                    SetupColorTexture(camera, descriptor, out RenderTexture color);

                    if (temporalData.ContainsKey(camera))
                    {
                        if (temporalData[camera].ColorTexture != null)
                        {
                            temporalData[camera].ColorTexture.Release();
                            temporalData[camera].ColorTexture = null;
                        }

                        temporalData[camera].ColorTexture = color;
                    }
                    else
                    {
                        temporalData.Add(camera, new TAACameraData(TimeManager.FrameCount, color, camera.name));
                    }
                }

                private void SetupColorTexture(Camera camera, RenderTextureDescriptor descriptor, out RenderTexture renderTexture)
                {
                    descriptor.colorFormat = RenderTextureFormat.DefaultHDR;
                    descriptor.depthBufferBits = 24;
                    descriptor.msaaSamples = 1;
                    descriptor.useDynamicScale = false;

                    renderTexture = new RenderTexture(descriptor);
                    renderTexture.name = camera.name + " Color History";
                    renderTexture.filterMode = FilterMode.Point;
                    renderTexture.wrapMode = TextureWrapMode.Clamp;
                    renderTexture.Create();

                    Helpers.ClearRenderTexture(ref renderTexture);
                }

                private void CleanupDictionary()
                {
                    removeTargets.Clear();
                    foreach (KeyValuePair<Camera, TAACameraData> entry in temporalData)
                    {
                        if (entry.Value.LastFrameUsed < TimeManager.FrameCount - 10)
                        {
                            if (entry.Value.ColorTexture != null)
                            {
                                entry.Value.ColorTexture.Release();
                                entry.Value.ColorTexture = null;
                            }

                            removeTargets.Add(entry.Key);
                        }
                    }

                    for (int i = 0; i < removeTargets.Count; i++)
                    {
                        temporalData.Remove(removeTargets[i]);
                    }
                }

                public struct ProjectionMatrices
                {
                    public Matrix4x4 viewProjection;
                    public Matrix4x4 prevViewProjection;
                    public Matrix4x4 projection;
                    public Matrix4x4 inverseProjection;
                }

                public ProjectionMatrices SetupMatrices(RenderingData renderingData)
                {
                    ProjectionMatrices m;

                    //m.projection = renderingData.cameraData.GetGPUProjectionMatrix(0);
                    m.projection = renderingData.cameraData.camera.nonJitteredProjectionMatrix;
                    m.inverseProjection = m.projection.inverse;

                    var view = renderingData.cameraData.camera.worldToCameraMatrix;
                    m.viewProjection = m.projection * view;

                    m.prevViewProjection = GetPreviousViewProjection(renderingData.cameraData.camera);
                    SetPreviousViewProjection(renderingData.cameraData.camera, m.viewProjection);

                    return m;
                }

                public Matrix4x4 GetPreviousViewProjection(Camera camera)
                {
                    if (temporalData.TryGetValue(camera, out TAACameraData data))
                    {
                        return data.PrevViewProj;
                    }
                    else
                    {
                        return Matrix4x4.identity;
                    }
                }

                public void SetPreviousViewProjection(Camera camera, Matrix4x4 currentViewProjection)
                {
                    if (temporalData.ContainsKey(camera))
                    {
                        temporalData[camera].PrevViewProj = currentViewProjection;
                    }
                }
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                CloudShaderParamHandler.SetCloudMaterialSettings(cmd, skyDirector.cloudDefinition);

                RenderTextureDescriptor rtDescriptor = cameraTextureDescriptor;
                AssignDefaultDescriptorSettings(ref rtDescriptor);

                ConfigureCloudRendering(rtDescriptor);
                ConfigureDepth(cmd, cameraTextureDescriptor);
                ConfigureUpscaling(cmd, rtDescriptor);

                RenderingUtils.ReAllocateIfNeeded(ref temporalTarget, rtDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: taaId);
                RenderingUtils.ReAllocateIfNeeded(ref mergeTarget, rtDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: mergeId);

                void ConfigureCloudRendering(RenderTextureDescriptor descriptor)
                {
                    cloudRenderDescriptor = descriptor;
                    cloudRenderDescriptor.height = (int)(cloudRenderDescriptor.height * skyDirector.cloudDefinition.renderScale);
                    cloudRenderDescriptor.width = (int)(cloudRenderDescriptor.width * skyDirector.cloudDefinition.renderScale);
                    RenderingUtils.ReAllocateIfNeeded(ref cloudTarget, cloudRenderDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: cloudId);
                }
                void ConfigureDepth(CommandBuffer cmd, RenderTextureDescriptor descriptor)
                {
                    RenderTextureDescriptor depthDescriptor = descriptor;
                    AssignDefaultDescriptorSettings(ref depthDescriptor, RenderTextureFormat.RFloat);
                    RenderingUtils.ReAllocateIfNeeded(ref depthTex, depthDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: depthId);
                }
                void ConfigureUpscaling(CommandBuffer cmd, RenderTextureDescriptor descriptor)
                {
                    if (
                        skyDirector.cloudDefinition.renderScaleSelection == RenderScaleSelection.Half
                        || skyDirector.cloudDefinition.renderScaleSelection == RenderScaleSelection.Quarter
                    )
                    {
                        RenderingUtils.ReAllocateIfNeeded(
                            ref upscaleHalfRes,
                            descriptor,
                            FilterMode.Point,
                            TextureWrapMode.Clamp,
                            name: upscaleHalfId
                        );
                    }

                    if (skyDirector.cloudDefinition.renderScaleSelection == RenderScaleSelection.Quarter)
                    {
                        RenderTextureDescriptor halfResDescriptor = descriptor;
                        halfResDescriptor.width = (int)(descriptor.width * 0.5f);
                        halfResDescriptor.height = (int)(descriptor.height * 0.5f);

                        RenderingUtils.ReAllocateIfNeeded(
                            ref upscaleQuarterRes,
                            descriptor,
                            FilterMode.Point,
                            TextureWrapMode.Clamp,
                            name: upscaleQuarterId
                        );
                    }
                }
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                Profiler.BeginSample(profilerTag);
                CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

                TimeManager.Update();
                RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;
                RTHandle depth = renderingData.cameraData.renderer.cameraDepthTargetHandle;

                Vector3 cameraPosition = renderingData.cameraData.worldSpaceCameraPos;
                SetupGlobalShaderParams();

                RenderClouds(cmd);
                UpscaleClouds(cmd, out RenderTargetIdentifier taaInput);
                TemporalAntiAliasing(cmd, renderingData, taaInput);
                Merge(cmd);

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
                Profiler.EndSample();

                void SetupGlobalShaderParams()
                {
                    cmd.SetGlobalTexture(CloudShaderParamHandler.ShaderParams._Halton_23_Sequence, Helpers.GetHaltonSequence(skyDirector.data));
                    cmd.SetGlobalTexture(CloudShaderParamHandler.ShaderParams._BLUE_NOISE, Helpers.GetBlueNoise(skyDirector.data));
                    cmd.SetGlobalInt(CloudShaderParamHandler.ShaderParams._FrameId, TimeManager.FrameCount);
                    cmd.SetGlobalVector(CloudShaderParamHandler.ShaderParams._MainCameraOrigin, cameraPosition);
                    cmd.SetGlobalFloat(CloudShaderParamHandler.ShaderParams.Shadows._CloudShadowStrength, skyDirector.cloudDefinition.shadowStrength);
                }
                void RenderClouds(CommandBuffer cmd)
                {
                    CloudShaderParamHandler.SetDepthCulling(cmd, skyDirector.cloudDefinition);

                    cmd.SetGlobalFloat(CloudShaderParamHandler.ShaderParams._CLOUD_RENDER_SCALE, skyDirector.cloudDefinition.renderScale);
                    bool useDitheredDepth =
                        skyDirector.cloudDefinition.depthCullOptions == DepthCullOptions.RenderLocal
                        && skyDirector.cloudDefinition.renderScaleSelection != RenderScaleSelection.Full
                            ? true
                            : false;
                    cmd.SetGlobalInt(CloudShaderParamHandler.ShaderParams._USE_DITHERED_DEPTH, useDitheredDepth ? 1 : 0);

                    Blitter.BlitCameraTexture(cmd, source, depthTex, ditherDepth, 0);

                    cmd.SetGlobalTexture(CloudShaderParamHandler.ShaderParams._DitheredDepthTexture, depthTex.nameID);
                    cmd.SetGlobalInt(CloudShaderParamHandler.ShaderParams._ShadowPass, 0);
                    cmd.SetGlobalVector(
                        CloudShaderParamHandler.ShaderParams._RenderTextureDimensions,
                        new Vector4(
                            1f / cloudRenderDescriptor.width,
                            1f / cloudRenderDescriptor.height,
                            cloudRenderDescriptor.width,
                            cloudRenderDescriptor.height
                        )
                    );

                    cmd.SetRenderTarget(cloudTarget.nameID);
                    Blitter.BlitTexture(cmd, new Vector4(1, 1, 0, 0), cloudRenderMaterial, 0);
                }
                void UpscaleClouds(CommandBuffer cmd, out RenderTargetIdentifier taaInput)
                {
                    RTHandle upscaleInput = cloudTarget;
                    taaInput = upscaleInput;

                    if (skyDirector.cloudDefinition.renderScaleSelection != RenderScaleSelection.Full)
                    {
                        if (skyDirector.cloudDefinition.renderScaleSelection == RenderScaleSelection.Quarter)
                        {
                            cmd.SetGlobalFloat(CloudShaderParamHandler.ShaderParams._UPSCALE_SOURCE_RENDER_SCALE, 0.25f);
                            cmd.SetGlobalTexture(CloudShaderParamHandler.ShaderParams._ScreenTexture, upscaleInput);

                            Blitter.BlitCameraTexture(cmd, source, upscaleQuarterRes, upscale, 0);

                            cmd.SetGlobalFloat(CloudShaderParamHandler.ShaderParams._UPSCALE_SOURCE_RENDER_SCALE, 0.5f);
                            cmd.SetGlobalTexture(CloudShaderParamHandler.ShaderParams._ScreenTexture, upscaleQuarterRes);
                            Blitter.BlitCameraTexture(cmd, source, upscaleHalfRes, upscale, 0);

                            taaInput = upscaleHalfRes;
                        }

                        if (skyDirector.cloudDefinition.renderScaleSelection == RenderScaleSelection.Half)
                        {
                            cmd.SetGlobalFloat(CloudShaderParamHandler.ShaderParams._UPSCALE_SOURCE_RENDER_SCALE, 0.5f);
                            cmd.SetGlobalTexture(CloudShaderParamHandler.ShaderParams._ScreenTexture, upscaleInput);

                            Blitter.BlitCameraTexture(cmd, source, upscaleHalfRes, upscale, 0);
                            taaInput = upscaleHalfRes;
                        }
                    }
                }
                void TemporalAntiAliasing(CommandBuffer cmd, RenderingData renderingData, RenderTargetIdentifier taaInput)
                {
                    Camera camera = renderingData.cameraData.camera;
                    if (!skyDirector.cloudDefinition.taaEnabled || skyDirector.cloudDefinition.taaBlendFactor >= 1.0f)
                    {
                        cmd.SetGlobalTexture(mergePassInputTextureShaderReference, taaInput);
                        return;
                    }

                    TemporalAA.ProjectionMatrices matrices = taa.SetupMatrices(renderingData);
                    cmd.SetGlobalMatrix(CloudShaderParamHandler.ShaderParams._ViewProjM, matrices.viewProjection);
                    cmd.SetGlobalMatrix(CloudShaderParamHandler.ShaderParams._PrevViewProjM, matrices.prevViewProjection);

                    bool isTemporalDataValid = taa.IsTemporalDataValid(camera, renderingData.cameraData.cameraTargetDescriptor);
                    if (!isTemporalDataValid)
                    {
                        taa.SetupTemporalData(camera, renderingData.cameraData.cameraTargetDescriptor);
                        CloudShaderParamHandler.IgnoreTAAThisFrame(cmd);
                    }
                    else
                    {
                        CloudShaderParamHandler.ConfigureTAAParams(cmd, skyDirector.cloudDefinition);
                        cmd.SetGlobalTexture(colorHistoryId, taa.TemporalData[camera].ColorTexture);
                        taa.TemporalData[camera].LastFrameUsed = TimeManager.FrameCount;
                    }

                    cmd.SetGlobalTexture("_CURRENT_TAA_FRAME", taaInput);

                    Blitter.BlitCameraTexture(cmd, source, temporalTarget, cloudTaa, 0);
                    Blitter.BlitCameraTexture(cmd, temporalTarget, RTHandles.Alloc(taa.TemporalData[camera].ColorTexture));

                    cmd.SetGlobalTexture(mergePassInputTextureShaderReference, temporalTarget);

                    taa.TemporalData[camera].LastFrameUsed = TimeManager.FrameCount;
                }

                void Merge(CommandBuffer cmd)
                {
                    cmd.SetGlobalTexture(CloudShaderParamHandler.ShaderParams._ScreenTexture, source);
                    Blitter.BlitCameraTexture(cmd, source, mergeTarget, merge, 0);
                    Blitter.BlitCameraTexture(cmd, mergeTarget, source);
                }
            }

            public override void FrameCleanup(CommandBuffer cmd)
            {
                taa.Cleanup();
            }
        }

        private class CloudShadowsRenderPass : ScriptableRenderPass
        {
            private RTHandle shadowmapTarget;
            private RTHandle screenShadowsTarget;
            private RTHandle mergeTarget;

            private const string shadowmapId = "_CloudShadowmap";
            private const string screenShadowsId = "_CloudScreenShadows";
            private const string mergeId = "_CloudShadowsOnScreen";

            private Material screenShadows;
            private Material shadowsToScreen;

            private const string profilerTag = "Altos: Cloud Shadows";

            public CloudShadowsRenderPass()
            {
                shadowmapTarget = RTHandles.Alloc(Shader.PropertyToID(shadowmapId), name: shadowmapId);
                screenShadowsTarget = RTHandles.Alloc(Shader.PropertyToID(screenShadowsId), name: screenShadowsId);
                mergeTarget = RTHandles.Alloc(Shader.PropertyToID(mergeId), name: mergeId);
            }

            public void Dispose()
            {
                CoreUtils.Destroy(screenShadows);
                CoreUtils.Destroy(shadowsToScreen);

                screenShadows = null;
                shadowsToScreen = null;
            }

            public void Setup()
            {
                if (screenShadows == null)
                    screenShadows = CoreUtils.CreateEngineMaterial(skyDirector.data.shaders.screenShadows);
                if (shadowsToScreen == null)
                    shadowsToScreen = CoreUtils.CreateEngineMaterial(skyDirector.data.shaders.renderShadowsToScreen);
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                RenderTextureDescriptor rtDescriptor = cameraTextureDescriptor;
                AssignDefaultDescriptorSettings(ref rtDescriptor);

                ConfigureShadows(rtDescriptor);
                ConfigureScreenShadows(rtDescriptor);
                RenderingUtils.ReAllocateIfNeeded(ref mergeTarget, rtDescriptor, FilterMode.Bilinear, TextureWrapMode.Clamp, name: mergeId);

                void ConfigureShadows(RenderTextureDescriptor descriptor)
                {
                    if (skyDirector.cloudDefinition.castShadowsEnabled)
                    {
                        descriptor.width = (int)skyDirector.cloudDefinition.shadowResolution;
                        descriptor.height = (int)skyDirector.cloudDefinition.shadowResolution;
                        descriptor.colorFormat = RenderTextureFormat.DefaultHDR;

                        RenderingUtils.ReAllocateIfNeeded(
                            ref shadowmapTarget,
                            descriptor,
                            FilterMode.Bilinear,
                            TextureWrapMode.Clamp,
                            name: shadowmapId
                        );
                    }
                }

                void ConfigureScreenShadows(RenderTextureDescriptor descriptor)
                {
                    RenderingUtils.ReAllocateIfNeeded(
                        ref screenShadowsTarget,
                        descriptor,
                        FilterMode.Bilinear,
                        TextureWrapMode.Clamp,
                        name: screenShadowsId
                    );
                }
            }

            private struct SunData
            {
                public Vector3 forward;
                public Vector3 right;
                public Vector3 up;
            }

            bool GetSunData(out SunData sunData)
            {
                sunData = new SunData();

                if (skyDirector?.Sun != null)
                {
                    Transform child = skyDirector.Sun.GetChild();
                    sunData.forward = child.forward;
                    sunData.up = child.up;
                    sunData.right = child.right;
                    return true;
                }
                else
                {
                    SetMainLightShaderProperties setMainLightShaderProperties = SetMainLightShaderProperties.Instance;
                    if (setMainLightShaderProperties != null)
                    {
                        sunData.forward = setMainLightShaderProperties.transform.forward;
                        sunData.up = setMainLightShaderProperties.transform.up;
                        sunData.right = setMainLightShaderProperties.transform.right;
                        return true;
                    }
                }
                return false;
            }

            Vector3 Div(Vector3 a, float b)
            {
                return new Vector3(a.x / b, a.y / b, a.z / b);
            }

            Vector3 Floor(Vector3 i)
            {
                return new Vector3(Mathf.Floor(i.x), Mathf.Floor(i.y), Mathf.Floor(i.z));
            }

            private readonly static float[] _ShadowMapCascades = { 0.0625f, 0.25f, 0.5625f, 1.0f };
            Matrix4x4[] projMatrices = new Matrix4x4[4];
            Matrix4x4[] viewMatrices = new Matrix4x4[4];
            Matrix4x4[] worldToShadowMatrices = new Matrix4x4[4];
            Vector4[] shadowCameraPosition = new Vector4[4];

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                Profiler.BeginSample(profilerTag);
                CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

                RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;

                if (GetSunData(out SunData sunData))
                {
                    RenderShadows(cmd, renderingData, sunData);
                    DrawScreenSpaceShadows(cmd);
                    if (skyDirector.cloudDefinition.screenShadows)
                    {
                        RenderToScreen(cmd);
                    }
                }

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);
                Profiler.EndSample();

                void RenderShadows(CommandBuffer cmd, RenderingData renderingData, SunData sunData)
                {
                    float zFar = 60000f;

                    float halfWidth = skyDirector.cloudDefinition.shadowDistance;

                    for (int i = 0; i < 4; i++)
                    {
                        Vector3 playerCameraPosition = renderingData.cameraData.worldSpaceCameraPos;
                        Vector3 shadowCasterCameraPosition = playerCameraPosition - sunData.forward * zFar * 0.5f;

                        // Prevent shimmering when the camera moves
                        Vector3 min =
                            shadowCasterCameraPosition
                            - halfWidth * _ShadowMapCascades[i] * sunData.right
                            - halfWidth * _ShadowMapCascades[i] * sunData.up;

                        Vector3 max =
                            shadowCasterCameraPosition
                            + sunData.forward * zFar
                            + halfWidth * _ShadowMapCascades[i] * sunData.right
                            + halfWidth * _ShadowMapCascades[i] * sunData.up;

                        float radius = (new Vector2(max.x, max.z) - new Vector2(min.x, min.z)).magnitude / 2f;
                        float texelSize = radius / (0.25f * (int)skyDirector.cloudDefinition.shadowResolution);

                        playerCameraPosition = Floor(Div(playerCameraPosition, texelSize));
                        playerCameraPosition = playerCameraPosition * texelSize;

                        shadowCasterCameraPosition = playerCameraPosition - sunData.forward * zFar * 0.5f;

                        viewMatrices[i] = MatrixHandler.SetupViewMatrix(shadowCasterCameraPosition, sunData.forward, zFar, sunData.up);
                        shadowCameraPosition[i] = shadowCasterCameraPosition;
                    }

                    for (int i = 0; i < 4; i++)
                    {
                        projMatrices[i] = MatrixHandler.SetupProjectionMatrix(halfWidth * _ShadowMapCascades[i], zFar);
                    }

                    cmd.SetGlobalFloat(CloudShaderParamHandler.ShaderParams.Shadows._ShadowRadius, halfWidth);
                    cmd.SetGlobalFloatArray(CloudShaderParamHandler.ShaderParams.Shadows._ShadowMapCascades, _ShadowMapCascades);

                    cmd.SetGlobalVector(
                        CloudShaderParamHandler.ShaderParams.Shadows._CloudShadowOrthoParams,
                        new Vector4(halfWidth * 2, halfWidth * 2, zFar, 0)
                    );

                    cmd.SetGlobalVectorArray(CloudShaderParamHandler.ShaderParams.Shadows._ShadowCasterCameraPosition, shadowCameraPosition);

                    for (int i = 0; i < 4; i++)
                    {
                        worldToShadowMatrices[i] = MatrixHandler.ConvertToWorldToShadowMatrix(projMatrices[i], viewMatrices[i]);
                    }

                    cmd.SetGlobalMatrixArray(CloudShaderParamHandler.ShaderParams.Shadows._CloudShadow_WorldToShadowMatrix, worldToShadowMatrices);
                    cmd.SetGlobalFloat(
                        CloudShaderParamHandler.ShaderParams.Shadows._ShadowRenderStepCount,
                        skyDirector.cloudDefinition.shadowStepCount
                    );
                    cmd.SetGlobalFloat(CloudShaderParamHandler.ShaderParams.Shadows._CloudShadowDistance, skyDirector.cloudDefinition.shadowDistance);
                    cmd.SetGlobalVector(CloudShaderParamHandler.ShaderParams.Shadows._ShadowCasterCameraForward, sunData.forward);
                    cmd.SetGlobalVector(CloudShaderParamHandler.ShaderParams.Shadows._ShadowCasterCameraUp, sunData.up);
                    cmd.SetGlobalVector(CloudShaderParamHandler.ShaderParams.Shadows._ShadowCasterCameraRight, sunData.right);
                    cmd.SetGlobalInt(CloudShaderParamHandler.ShaderParams._ShadowPass, 1);
                    cmd.SetGlobalVector(
                        CloudShaderParamHandler.ShaderParams._RenderTextureDimensions,
                        new Vector4(
                            1f / (int)skyDirector.cloudDefinition.shadowResolution,
                            1f / (int)skyDirector.cloudDefinition.shadowResolution,
                            (int)skyDirector.cloudDefinition.shadowResolution,
                            (int)skyDirector.cloudDefinition.shadowResolution
                        )
                    );

                    cmd.SetGlobalVector(
                        CloudShaderParamHandler.ShaderParams.Shadows._ShadowmapResolution,
                        new Vector4(
                            (int)skyDirector.cloudDefinition.shadowResolution,
                            (int)skyDirector.cloudDefinition.shadowResolution,
                            1f / (int)skyDirector.cloudDefinition.shadowResolution,
                            1f / (int)skyDirector.cloudDefinition.shadowResolution
                        )
                    );

                    Blitter.BlitCameraTexture(cmd, source, shadowmapTarget, cloudRenderMaterial, 0);

                    cmd.SetGlobalTexture(CloudShaderParamHandler.ShaderParams.Shadows._CloudShadowmap, shadowmapTarget);
                }

                void DrawScreenSpaceShadows(CommandBuffer cmd)
                {
                    cmd.SetGlobalInt(CloudShaderParamHandler.ShaderParams._CastScreenCloudShadows, skyDirector.cloudDefinition.screenShadows ? 1 : 0);
                    cmd.SetGlobalTexture(CloudShaderParamHandler.ShaderParams._ScreenTexture, source);
                    Blitter.BlitCameraTexture(cmd, source, screenShadowsTarget, screenShadows, 0);
                }
                void RenderToScreen(CommandBuffer cmd)
                {
                    cmd.SetGlobalTexture(CloudShaderParamHandler.ShaderParams._ScreenTexture, source);
                    cmd.SetGlobalTexture(CloudShaderParamHandler.ShaderParams.Shadows._CloudScreenShadows, screenShadowsTarget);
                    Blitter.BlitCameraTexture(cmd, source, mergeTarget, shadowsToScreen, 0);
                    Blitter.BlitCameraTexture(cmd, mergeTarget, source);
                }
            }
        }

        private class AtmosphereBlendingPass : ScriptableRenderPass
        {
            private Material blendingMaterial;
            private RTHandle blendingTarget;

            private const string blendingTargetId = "_AltosFogTarget";
            private const string profilerTag = "Altos: Atmosphere Blending";

            public AtmosphereBlendingPass()
            {
                blendingTarget = RTHandles.Alloc(Shader.PropertyToID(blendingTargetId), name: blendingTargetId);
            }

            public void Dispose()
            {
                CoreUtils.Destroy(blendingMaterial);
                blendingMaterial = null;
            }

            public void Setup()
            {
                if (blendingMaterial == null)
                    blendingMaterial = CoreUtils.CreateEngineMaterial(skyDirector.data.shaders.atmosphereBlending);
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                RenderTextureDescriptor rtDescriptor = cameraTextureDescriptor;
                rtDescriptor.msaaSamples = 1;
                rtDescriptor.depthBufferBits = 0;

                RenderingUtils.ReAllocateIfNeeded(ref blendingTarget, rtDescriptor, FilterMode.Point, TextureWrapMode.Clamp, name: blendingTargetId);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                Profiler.BeginSample(profilerTag);
                CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

                RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;

                cmd.SetGlobalFloat("_Density", skyDirector.atmosphereDefinition.GetDensity());
                cmd.SetGlobalTexture(CloudShaderParamHandler.ShaderParams._ScreenTexture, source);
                Blitter.BlitCameraTexture(cmd, source, blendingTarget, blendingMaterial, 0);

                Blitter.BlitCameraTexture(cmd, blendingTarget, source);

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);

                Profiler.EndSample();
            }
        }

        private SkyRenderPass skyRenderPass;
        private AtmosphereBlendingPass atmospherePass;
        private VolumetricCloudsRenderPass cloudRenderPass;
        private CloudShadowsRenderPass shadowRenderPass;

        internal static Material cloudRenderMaterial = null;

        protected override void Dispose(bool disposing)
        {
            skyRenderPass?.Dispose();
            atmospherePass?.Dispose();
            cloudRenderPass?.Dispose();
            shadowRenderPass?.Dispose();

            skyRenderPass = null;
            atmospherePass = null;
            cloudRenderPass = null;
            shadowRenderPass = null;
            CoreUtils.Destroy(cloudRenderMaterial);
        }

        private void OnEnable()
        {
            Helpers.RenderFeatureOnEnable(Recreate);
        }

        private void OnDisable()
        {
            Helpers.RenderFeatureOnDisable(Recreate);
        }

        private void Recreate(UnityEngine.SceneManagement.Scene current, UnityEngine.SceneManagement.Scene next)
        {
            Create();
        }

        public override void Create()
        {
            skyRenderPass = new SkyRenderPass();
            skyRenderPass.renderPassEvent = RenderPassEvent.BeforeRenderingPrePasses;

            shadowRenderPass = new CloudShadowsRenderPass();
            shadowRenderPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

            atmospherePass = new AtmosphereBlendingPass();
            atmospherePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques + 1;

            cloudRenderPass = new VolumetricCloudsRenderPass();
            cloudRenderPass.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
        }

        internal static AltosSkyDirector skyDirector;

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.camera.TryGetComponent(out DisableAltosRendering _))
            {
                return;
            }

            skyDirector = AltosSkyDirector.Instance;

            if (skyDirector == null)
                return;

            if (PassValidator.IsValidSkyPass(renderingData.cameraData.camera))
            {
                skyRenderPass.Setup();
                renderer.EnqueuePass(skyRenderPass);
            }

            if (PassValidator.IsValidCloudPass(renderingData.cameraData.camera))
            {
                if (cloudRenderMaterial == null)
                    cloudRenderMaterial = CoreUtils.CreateEngineMaterial(skyDirector.data.shaders.renderClouds);

                SetDefaultCloudSettings();
                cloudRenderPass.Setup();
                renderer.EnqueuePass(cloudRenderPass);

                if (skyDirector.cloudDefinition.castShadowsEnabled)
                {
                    shadowRenderPass.Setup();
                    renderer.EnqueuePass(shadowRenderPass);
                }
            }

            if (PassValidator.IsValidAtmospherePass(renderingData.cameraData.camera))
            {
                atmospherePass.Setup();
                renderer.EnqueuePass(atmospherePass);
            }
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            skyRenderPass.ConfigureInput(ScriptableRenderPassInput.Depth);
            cloudRenderPass.ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
            shadowRenderPass.ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
            atmospherePass.ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
        }

        private void SetDefaultCloudSettings()
        {
            Shader.SetGlobalInt(ShaderParams._HasSkyTexture, 0);
            Shader.SetGlobalColor(ShaderParams._HorizonColor, RenderSettings.ambientEquatorColor);
            Shader.SetGlobalColor(ShaderParams._ZenithColor, RenderSettings.ambientSkyColor);
        }

        private static class ShaderParams
        {
            public static int _HasSkyTexture = Shader.PropertyToID("_HasSkyTexture");
            public static int _HorizonColor = Shader.PropertyToID("_HorizonColor");
            public static int _ZenithColor = Shader.PropertyToID("_ZenithColor");
        }

        private static class PassValidator
        {
            public static bool IsValidCloudPass(Camera camera)
            {
                if (skyDirector.cloudDefinition == null)
                    return false;

#if UNITY_EDITOR
                if (Check.IsPreviewCamera(camera))
                    return false;

                if (Check.IsSceneCamera(camera) && skyDirector.cloudDefinition.renderInSceneView)
                {
                    if (!Check.IsSkyboxEnabled)
                        return false;

                    if (!Check.IsDrawingTextured)
                        return false;

                    if (Check.IsPrefabStage())
                        return false;
                }
#endif

                if (Check.IsReflectionCamera(camera))
                    return false;

                return true;
            }

            public static bool IsValidSkyPass(Camera c)
            {
#if UNITY_EDITOR
                if (Check.IsPreviewCamera(c))
                    return false;

                if (Check.IsSceneCamera(c))
                {
                    if (!Check.IsSkyboxEnabled)
                        return false;

                    if (!Check.IsDrawingTextured)
                        return false;

                    if (Check.IsPrefabStage())
                        return false;
                }
#endif

                if (skyDirector.skyDefinition == null)
                    return false;

                return true;
            }

            public static bool IsValidAtmospherePass(Camera c)
            {
#if UNITY_EDITOR
                if (Check.IsPreviewCamera(c))
                    return false;

                if (Check.IsSceneCamera(c))
                {
                    if (!Check.IsFogEnabled)
                        return false;

                    if (!Check.IsDrawingTextured)
                        return false;

                    if (Check.IsPrefabStage())
                        return false;
                }
#endif

                if (Check.IsReflectionCamera(c))
                    return false;

                if (skyDirector.skyDefinition == null)
                    return false;

                if (skyDirector.atmosphereDefinition == null)
                    return false;

                return true;
            }

            private static class Check
            {
                public static bool IsReflectionCamera(Camera c)
                {
                    return c.cameraType == CameraType.Reflection;
                }

#if UNITY_EDITOR
                public static bool IsPreviewCamera(Camera c)
                {
                    return c.cameraType == CameraType.Preview;
                }

                public static bool IsSceneCamera(Camera c)
                {
                    return c.cameraType == CameraType.SceneView;
                }

                public static bool IsSkyboxEnabled
                {
                    get => UnityEditor.SceneView.currentDrawingSceneView.sceneViewState.skyboxEnabled;
                }

                public static bool IsFogEnabled
                {
                    get => UnityEditor.SceneView.currentDrawingSceneView.sceneViewState.fogEnabled;
                }

                public static bool IsDrawingTextured
                {
                    get => UnityEditor.SceneView.currentDrawingSceneView.cameraMode.drawMode == UnityEditor.DrawCameraMode.Textured;
                }

                public static bool IsPrefabStage()
                {
                    return UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null;
                }
#endif
            }
        }

        private static class MatrixHandler
        {
            public static Matrix4x4 SetupViewMatrix(Vector3 cameraPosition, Vector3 cameraForward, float zFar, Vector3 cameraUp)
            {
                // It is extremely important that the LookAt matrix uses real positions, not just relative vectors, for the "from" and "to" fields.
                Matrix4x4 lookMatrix = Matrix4x4.LookAt(cameraPosition, cameraPosition + cameraForward * zFar, cameraUp);
                Matrix4x4 scaleMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(1, 1, -1));
                Matrix4x4 viewMatrix = scaleMatrix * lookMatrix.inverse;
                return viewMatrix;
            }

            public static Matrix4x4 SetupProjectionMatrix(float halfWidth, float zFar)
            {
                float s = halfWidth;
                Matrix4x4 proj = Matrix4x4.Ortho(-s, s, -s, s, 30, zFar);
                return proj;
            }

            public static Matrix4x4 ConvertToWorldToShadowMatrix(Matrix4x4 projectionMatrix, Matrix4x4 viewMatrix)
            {
                /*
                 * This was causing a problem when using cloud shadows on DX11
                if (SystemInfo.usesReversedZBuffer)
                {
                    projectionMatrix.m20 = -projectionMatrix.m20;
                    projectionMatrix.m21 = -projectionMatrix.m21;
                    projectionMatrix.m22 = -projectionMatrix.m22;
                    projectionMatrix.m23 = -projectionMatrix.m23;
                }
                */
                var scaleOffset = Matrix4x4.identity;
                scaleOffset.m00 = scaleOffset.m11 = scaleOffset.m22 = 0.5f;
                scaleOffset.m03 = scaleOffset.m13 = scaleOffset.m23 = 0.5f;
                return scaleOffset * (projectionMatrix * viewMatrix);
            }
        }

        private class CloudShaderParamHandler
        {
            public static class ShaderParams
            {
                public static int _ScreenTexture = Shader.PropertyToID("_ScreenTexture");
                public static int _CLOUD_RENDER_SCALE = Shader.PropertyToID("_CLOUD_RENDER_SCALE");
                public static int depthCullReference = Shader.PropertyToID("_CLOUD_DEPTH_CULL_ON");
                public static int taaBlendFactorReference = Shader.PropertyToID("_TAA_BLEND_FACTOR");
                public static int _ViewProjM = Shader.PropertyToID("_ViewProjM");
                public static int _PrevViewProjM = Shader.PropertyToID("_PrevViewProjM");
                public static int _UPSCALE_SOURCE_RENDER_SCALE = Shader.PropertyToID("_UPSCALE_SOURCE_RENDER_SCALE");

                public static int _CastScreenCloudShadows = Shader.PropertyToID("_CastScreenCloudShadows");

                public static int _Halton_23_Sequence = Shader.PropertyToID("_Halton_23_Sequence");
                public static int _BLUE_NOISE = Shader.PropertyToID("_BLUE_NOISE");
                public static int _FrameId = Shader.PropertyToID("_FrameId");
                public static int _MainCameraOrigin = Shader.PropertyToID("_MainCameraOrigin");
                public static int _USE_DITHERED_DEPTH = Shader.PropertyToID("_USE_DITHERED_DEPTH");
                public static int _DitheredDepthTexture = Shader.PropertyToID("_DitheredDepthTexture");
                public static int _ShadowPass = Shader.PropertyToID("_ShadowPass");
                public static int _RenderTextureDimensions = Shader.PropertyToID("_RenderTextureDimensions");

                public static class Shadows
                {
                    public static int _CloudShadowOrthoParams = Shader.PropertyToID("_CloudShadowOrthoParams");
                    public static int _ShadowCasterCameraPosition = Shader.PropertyToID("_ShadowCasterCameraPosition");
                    public static int _CloudShadow_WorldToShadowMatrix = Shader.PropertyToID("_CloudShadow_WorldToShadowMatrix");
                    public static int _ShadowCasterCameraForward = Shader.PropertyToID("_ShadowCasterCameraForward");
                    public static int _ShadowCasterCameraUp = Shader.PropertyToID("_ShadowCasterCameraUp");
                    public static int _ShadowCasterCameraRight = Shader.PropertyToID("_ShadowCasterCameraRight");
                    public static int _CLOUD_SHADOW_PREVIOUS_HISTORY = Shader.PropertyToID("_CLOUD_SHADOW_PREVIOUS_HISTORY");
                    public static int _CLOUD_SHADOW_CURRENT_FRAME = Shader.PropertyToID("_CLOUD_SHADOW_CURRENT_FRAME");
                    public static int _IntegrationRate = Shader.PropertyToID("_IntegrationRate");
                    public static int _CloudShadowHistoryTexture = Shader.PropertyToID("_CloudShadowHistoryTexture");
                    public static int _ShadowRenderStepCount = Shader.PropertyToID("_ShadowRenderStepCount");
                    public static int _CloudShadowDistance = Shader.PropertyToID("_CloudShadowDistance");
                    public static int _ShadowMapCascades = Shader.PropertyToID("_ShadowMapCascades");
                    public static int _ShadowmapResolution = Shader.PropertyToID("_ShadowmapResolution");
                    public static int _ShadowRadius = Shader.PropertyToID("_ShadowRadius");
                    public static int _InverseViewProjectionM = Shader.PropertyToID("_InverseViewProjectionM");
                    public static int _CloudScreenShadows = Shader.PropertyToID("_CloudScreenShadows");

                    public static int _CloudShadowStrength = Shader.PropertyToID("_CloudShadowStrength");
                    public static int _CloudShadowmap = Shader.PropertyToID("_CloudShadowmap");
                }

                public static class CloudData
                {
                    public static int _CLOUD_STEP_COUNT = Shader.PropertyToID("_CLOUD_STEP_COUNT");
                    public static int _CLOUD_BLUE_NOISE_STRENGTH = Shader.PropertyToID("_CLOUD_BLUE_NOISE_STRENGTH");
                    public static int _CLOUD_BASE_TEX = Shader.PropertyToID("_CLOUD_BASE_TEX");
                    public static int _CLOUD_DETAIL1_TEX = Shader.PropertyToID("_CLOUD_DETAIL1_TEX");
                    public static int _CLOUD_EXTINCTION_COEFFICIENT = Shader.PropertyToID("_CLOUD_EXTINCTION_COEFFICIENT");
                    public static int _CLOUD_COVERAGE = Shader.PropertyToID("_CLOUD_COVERAGE");
                    public static int _CLOUD_SUN_COLOR_MASK = Shader.PropertyToID("_CLOUD_SUN_COLOR_MASK");
                    public static int _CLOUD_LAYER_HEIGHT = Shader.PropertyToID("_CLOUD_LAYER_HEIGHT");
                    public static int _CLOUD_LAYER_THICKNESS = Shader.PropertyToID("_CLOUD_LAYER_THICKNESS");
                    public static int _CLOUD_FADE_DIST = Shader.PropertyToID("_CLOUD_FADE_DIST");
                    public static int _CLOUD_BASE_SCALE = Shader.PropertyToID("_CLOUD_BASE_SCALE");
                    public static int _CLOUD_DETAIL1_SCALE = Shader.PropertyToID("_CLOUD_DETAIL1_SCALE");
                    public static int _CLOUD_DETAIL1_STRENGTH = Shader.PropertyToID("_CLOUD_DETAIL1_STRENGTH");
                    public static int _CLOUD_BASE_TIMESCALE = Shader.PropertyToID("_CLOUD_BASE_TIMESCALE");
                    public static int _CLOUD_DETAIL1_TIMESCALE = Shader.PropertyToID("_CLOUD_DETAIL1_TIMESCALE");
                    public static int _CLOUD_FOG_POWER = Shader.PropertyToID("_CLOUD_FOG_POWER");
                    public static int _CLOUD_MAX_LIGHTING_DIST = Shader.PropertyToID("_CLOUD_MAX_LIGHTING_DIST");
                    public static int _CLOUD_PLANET_RADIUS = Shader.PropertyToID("_CLOUD_PLANET_RADIUS");

                    public static int _CLOUD_CURL_TEX = Shader.PropertyToID("_CLOUD_CURL_TEX");
                    public static int _CLOUD_CURL_SCALE = Shader.PropertyToID("_CLOUD_CURL_SCALE");
                    public static int _CLOUD_CURL_STRENGTH = Shader.PropertyToID("_CLOUD_CURL_STRENGTH");
                    public static int _CLOUD_CURL_TIMESCALE = Shader.PropertyToID("_CLOUD_CURL_TIMESCALE");
                    public static int _CLOUD_CURL_ADJUSTMENT_BASE = Shader.PropertyToID("_CLOUD_CURL_ADJUSTMENT_BASE");

                    public static int _CLOUD_DETAIL2_TEX = Shader.PropertyToID("_CLOUD_DETAIL2_TEX");
                    public static int _CLOUD_DETAIL2_SCALE = Shader.PropertyToID("_CLOUD_DETAIL2_SCALE");
                    public static int _CLOUD_DETAIL2_TIMESCALE = Shader.PropertyToID("_CLOUD_DETAIL2_TIMESCALE");
                    public static int _CLOUD_DETAIL2_STRENGTH = Shader.PropertyToID("_CLOUD_DETAIL2_STRENGTH");

                    public static int _CLOUD_HGFORWARD = Shader.PropertyToID("_CLOUD_HGFORWARD");
                    public static int _CLOUD_HGBACK = Shader.PropertyToID("_CLOUD_HGBACK");
                    public static int _CLOUD_HGBLEND = Shader.PropertyToID("_CLOUD_HGBLEND");
                    public static int _CLOUD_HGSTRENGTH = Shader.PropertyToID("_CLOUD_HGSTRENGTH");

                    public static int _CLOUD_AMBIENT_EXPOSURE = Shader.PropertyToID("_CLOUD_AMBIENT_EXPOSURE");

                    public static int _CheapAmbientLighting = Shader.PropertyToID("_CheapAmbientLighting");

                    public static int _CLOUD_DISTANT_COVERAGE_START_DEPTH = Shader.PropertyToID("_CLOUD_DISTANT_COVERAGE_START_DEPTH");
                    public static int _CLOUD_DISTANT_CLOUD_COVERAGE = Shader.PropertyToID("_CLOUD_DISTANT_CLOUD_COVERAGE");
                    public static int _CLOUD_DETAIL1_HEIGHT_REMAP = Shader.PropertyToID("_CLOUD_DETAIL1_HEIGHT_REMAP");

                    public static int _CLOUD_DETAIL1_INVERT = Shader.PropertyToID("_CLOUD_DETAIL1_INVERT");
                    public static int _CLOUD_DETAIL2_HEIGHT_REMAP = Shader.PropertyToID("_CLOUD_DETAIL2_HEIGHT_REMAP");
                    public static int _CLOUD_DETAIL2_INVERT = Shader.PropertyToID("_CLOUD_DETAIL2_INVERT");
                    public static int _CLOUD_HEIGHT_DENSITY_INFLUENCE = Shader.PropertyToID("_CLOUD_HEIGHT_DENSITY_INFLUENCE");
                    public static int _CLOUD_COVERAGE_DENSITY_INFLUENCE = Shader.PropertyToID("_CLOUD_COVERAGE_DENSITY_INFLUENCE");

                    public static int _CLOUD_HIGHALT_TEX_1 = Shader.PropertyToID("_CLOUD_HIGHALT_TEX_1");
                    public static int _CLOUD_HIGHALT_TEX_2 = Shader.PropertyToID("_CLOUD_HIGHALT_TEX_2");
                    public static int _CLOUD_HIGHALT_TEX_3 = Shader.PropertyToID("_CLOUD_HIGHALT_TEX_3");

                    public static int _CLOUD_HIGHALT_OFFSET1 = Shader.PropertyToID("_CLOUD_HIGHALT_OFFSET1");
                    public static int _CLOUD_HIGHALT_OFFSET2 = Shader.PropertyToID("_CLOUD_HIGHALT_OFFSET2");
                    public static int _CLOUD_HIGHALT_OFFSET3 = Shader.PropertyToID("_CLOUD_HIGHALT_OFFSET3");
                    public static int _CLOUD_HIGHALT_SCALE1 = Shader.PropertyToID("_CLOUD_HIGHALT_SCALE1");
                    public static int _CLOUD_HIGHALT_SCALE2 = Shader.PropertyToID("_CLOUD_HIGHALT_SCALE2");
                    public static int _CLOUD_HIGHALT_SCALE3 = Shader.PropertyToID("_CLOUD_HIGHALT_SCALE3");
                    public static int _CLOUD_HIGHALT_COVERAGE = Shader.PropertyToID("_CLOUD_HIGHALT_COVERAGE");
                    public static int _CLOUD_HIGHALT_INFLUENCE1 = Shader.PropertyToID("_CLOUD_HIGHALT_INFLUENCE1");
                    public static int _CLOUD_HIGHALT_INFLUENCE2 = Shader.PropertyToID("_CLOUD_HIGHALT_INFLUENCE2");
                    public static int _CLOUD_HIGHALT_INFLUENCE3 = Shader.PropertyToID("_CLOUD_HIGHALT_INFLUENCE3");
                    public static int _CLOUD_BASE_RGBAInfluence = Shader.PropertyToID("_CLOUD_BASE_RGBAInfluence");
                    public static int _CLOUD_DETAIL1_RGBAInfluence = Shader.PropertyToID("_CLOUD_DETAIL1_RGBAInfluence");
                    public static int _CLOUD_DETAIL2_RGBAInfluence = Shader.PropertyToID("_CLOUD_DETAIL2_RGBAInfluence");
                    public static int _CLOUD_HIGHALT_EXTINCTION = Shader.PropertyToID("_CLOUD_HIGHALT_EXTINCTION");

                    public static int _CLOUD_HIGHALT_SHAPE_POWER = Shader.PropertyToID("_CLOUD_HIGHALT_SHAPE_POWER");
                    public static int _CLOUD_SCATTERING_AMPGAIN = Shader.PropertyToID("_CLOUD_SCATTERING_AMPGAIN");
                    public static int _CLOUD_SCATTERING_DENSITYGAIN = Shader.PropertyToID("_CLOUD_SCATTERING_DENSITYGAIN");
                    public static int _CLOUD_SCATTERING_OCTAVES = Shader.PropertyToID("_CLOUD_SCATTERING_OCTAVES");

                    public static int _CLOUD_SUBPIXEL_JITTER_ON = Shader.PropertyToID("_CLOUD_SUBPIXEL_JITTER_ON");
                    public static int _CLOUD_WEATHERMAP_TEX = Shader.PropertyToID("_CLOUD_WEATHERMAP_TEX");
                    public static int _CLOUD_WEATHERMAP_VELOCITY = Shader.PropertyToID("_CLOUD_WEATHERMAP_VELOCITY");
                    public static int _CLOUD_WEATHERMAP_SCALE = Shader.PropertyToID("_CLOUD_WEATHERMAP_SCALE");
                    public static int _CLOUD_WEATHERMAP_VALUE_RANGE = Shader.PropertyToID("_CLOUD_WEATHERMAP_VALUE_RANGE");
                    public static int _USE_CLOUD_WEATHERMAP_TEX = Shader.PropertyToID("_USE_CLOUD_WEATHERMAP_TEX");

                    public static int _CLOUD_DENSITY_CURVE_TEX = Shader.PropertyToID("_CLOUD_DENSITY_CURVE_TEX");

                    public static int _WEATHERMAP_OCTAVES = Shader.PropertyToID("_WEATHERMAP_OCTAVES");
                    public static int _WEATHERMAP_GAIN = Shader.PropertyToID("_WEATHERMAP_GAIN");
                    public static int _WEATHERMAP_LACUNARITY = Shader.PropertyToID("_WEATHERMAP_LACUNARITY");
                }
            }

            public static void SetDepthCulling(CommandBuffer cmd, CloudDefinition cloudDefinition)
            {
                cmd.SetGlobalInt(ShaderParams.depthCullReference, (int)cloudDefinition.depthCullOptions);
            }

            public static void ConfigureTAAParams(CommandBuffer cmd, CloudDefinition cloudDefinition)
            {
                cmd.SetGlobalFloat(ShaderParams.taaBlendFactorReference, cloudDefinition.taaBlendFactor);
            }

            public static void IgnoreTAAThisFrame(CommandBuffer cmd)
            {
                cmd.SetGlobalFloat(ShaderParams.taaBlendFactorReference, 1);
            }

            public static void SetCloudMaterialSettings(CommandBuffer cmd, CloudDefinition d)
            {
                cmd.SetGlobalFloat(ShaderParams._CLOUD_RENDER_SCALE, d.renderScale);

                cmd.SetGlobalFloat(ShaderParams.CloudData._CLOUD_AMBIENT_EXPOSURE, d.ambientExposure);
                cmd.SetGlobalInt(ShaderParams.CloudData._CheapAmbientLighting, d.cheapAmbientLighting ? 1 : 0);

                cmd.SetGlobalVector(ShaderParams.CloudData._CLOUD_BASE_SCALE, d.baseTextureScale);
                cmd.SetGlobalTexture(ShaderParams.CloudData._CLOUD_BASE_TEX, d.baseTexture);
                cmd.SetGlobalVector(ShaderParams.CloudData._CLOUD_BASE_TIMESCALE, d.baseTextureTimescale);
                cmd.SetGlobalFloat(ShaderParams.CloudData._CLOUD_BLUE_NOISE_STRENGTH, d.blueNoise);
                cmd.SetGlobalFloat(ShaderParams.CloudData._CLOUD_COVERAGE, d.cloudiness);
                cmd.SetGlobalFloat(ShaderParams.CloudData._CLOUD_COVERAGE_DENSITY_INFLUENCE, d.cloudinessDensityInfluence);

                cmd.SetGlobalFloat(ShaderParams.CloudData._CLOUD_CURL_SCALE, d.curlTextureScale);
                cmd.SetGlobalFloat(ShaderParams.CloudData._CLOUD_CURL_STRENGTH, d.curlTextureInfluence);
                cmd.SetGlobalTexture(ShaderParams.CloudData._CLOUD_CURL_TEX, d.curlTexture);
                cmd.SetGlobalFloat(ShaderParams.CloudData._CLOUD_CURL_TIMESCALE, d.curlTextureTimescale);

                cmd.SetGlobalVector(ShaderParams.CloudData._CLOUD_DETAIL1_HEIGHT_REMAP, d.detail1TextureHeightRemap);
                cmd.SetGlobalVector(ShaderParams.CloudData._CLOUD_DETAIL1_SCALE, d.detail1TextureScale);
                cmd.SetGlobalFloat(ShaderParams.CloudData._CLOUD_DETAIL1_STRENGTH, d.detail1TextureInfluence);
                cmd.SetGlobalTexture(ShaderParams.CloudData._CLOUD_DETAIL1_TEX, d.detail1Texture);
                cmd.SetGlobalVector(ShaderParams.CloudData._CLOUD_DETAIL1_TIMESCALE, d.detail1TextureTimescale);

                cmd.SetGlobalFloat(ShaderParams.CloudData._CLOUD_DISTANT_CLOUD_COVERAGE, d.distantCoverageAmount);
                cmd.SetGlobalFloat(ShaderParams.CloudData._CLOUD_DISTANT_COVERAGE_START_DEPTH, d.distantCoverageDepth);
                cmd.SetGlobalFloat(ShaderParams.CloudData._CLOUD_EXTINCTION_COEFFICIENT, d.extinctionCoefficient);
                cmd.SetGlobalFloat(ShaderParams.CloudData._CLOUD_FADE_DIST, d.cloudFadeDistance);
                cmd.SetGlobalFloat(ShaderParams.CloudData._CLOUD_FOG_POWER, d.GetAtmosphereAttenuationDensity());
                cmd.SetGlobalFloat(ShaderParams.CloudData._CLOUD_HEIGHT_DENSITY_INFLUENCE, d.heightDensityInfluence);

                cmd.SetGlobalFloat(ShaderParams.CloudData._CLOUD_HGFORWARD, d.HGEccentricityForward);
                cmd.SetGlobalFloat(ShaderParams.CloudData._CLOUD_HGBACK, d.HGEccentricityBackward);
                cmd.SetGlobalFloat(ShaderParams.CloudData._CLOUD_HGSTRENGTH, d.HGStrength);

                cmd.SetGlobalFloat(ShaderParams.CloudData._CLOUD_HIGHALT_COVERAGE, d.highAltCloudiness);
                cmd.SetGlobalFloat(ShaderParams.CloudData._CLOUD_HIGHALT_EXTINCTION, d.highAltExtinctionCoefficient);
                cmd.SetGlobalVector(ShaderParams.CloudData._CLOUD_HIGHALT_OFFSET1, d.highAltTimescale1);
                cmd.SetGlobalVector(ShaderParams.CloudData._CLOUD_HIGHALT_OFFSET2, d.highAltTimescale2);
                cmd.SetGlobalVector(ShaderParams.CloudData._CLOUD_HIGHALT_OFFSET3, d.highAltTimescale3);
                cmd.SetGlobalVector(ShaderParams.CloudData._CLOUD_HIGHALT_SCALE1, d.highAltScale1);
                cmd.SetGlobalVector(ShaderParams.CloudData._CLOUD_HIGHALT_SCALE2, d.highAltScale2);
                cmd.SetGlobalVector(ShaderParams.CloudData._CLOUD_HIGHALT_SCALE3, d.highAltScale3);
                cmd.SetGlobalTexture(ShaderParams.CloudData._CLOUD_HIGHALT_TEX_1, d.highAltTex1);
                cmd.SetGlobalTexture(ShaderParams.CloudData._CLOUD_HIGHALT_TEX_2, d.highAltTex2);
                cmd.SetGlobalTexture(ShaderParams.CloudData._CLOUD_HIGHALT_TEX_3, d.highAltTex3);

                cmd.SetGlobalFloat(ShaderParams.CloudData._CLOUD_LAYER_HEIGHT, d.cloudLayerHeight);
                cmd.SetGlobalFloat(ShaderParams.CloudData._CLOUD_LAYER_THICKNESS, d.cloudLayerThickness);
                cmd.SetGlobalInt(ShaderParams.CloudData._CLOUD_MAX_LIGHTING_DIST, d.maxLightingDistance);
                cmd.SetGlobalInt(ShaderParams.CloudData._CLOUD_PLANET_RADIUS, d.planetRadius);
                cmd.SetGlobalFloat(ShaderParams.CloudData._CLOUD_SCATTERING_AMPGAIN, d.multipleScatteringAmpGain);
                cmd.SetGlobalFloat(ShaderParams.CloudData._CLOUD_SCATTERING_DENSITYGAIN, d.multipleScatteringDensityGain);
                cmd.SetGlobalInt(ShaderParams.CloudData._CLOUD_SCATTERING_OCTAVES, d.multipleScatteringOctaves);
                cmd.SetGlobalInt(ShaderParams.CloudData._CLOUD_STEP_COUNT, d.stepCount);
                cmd.SetGlobalVector(ShaderParams.CloudData._CLOUD_SUN_COLOR_MASK, d.sunColor);

                cmd.SetGlobalInt(ShaderParams.CloudData._CLOUD_SUBPIXEL_JITTER_ON, d.subpixelJitterEnabled == true ? 1 : 0);
                cmd.SetGlobalTexture(ShaderParams.CloudData._CLOUD_WEATHERMAP_TEX, d.weathermapTexture);
                cmd.SetGlobalVector(ShaderParams.CloudData._CLOUD_WEATHERMAP_VELOCITY, d.weathermapVelocity);
                cmd.SetGlobalFloat(ShaderParams.CloudData._CLOUD_WEATHERMAP_SCALE, d.weathermapScale);
                cmd.SetGlobalInt(ShaderParams.CloudData._USE_CLOUD_WEATHERMAP_TEX, d.weathermapType == WeathermapType.Texture ? 1 : 0);

                cmd.SetGlobalTexture(ShaderParams.CloudData._CLOUD_DENSITY_CURVE_TEX, d.curve.GetTexture());

                cmd.SetGlobalFloat(ShaderParams.CloudData._WEATHERMAP_OCTAVES, d.weathermapOctaves);
                cmd.SetGlobalFloat(ShaderParams.CloudData._WEATHERMAP_GAIN, d.weathermapGain);
                cmd.SetGlobalFloat(ShaderParams.CloudData._WEATHERMAP_LACUNARITY, d.weathermapLacunarity);
            }
        }
    }
}
