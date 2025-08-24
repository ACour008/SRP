using UnityEngine;
using UnityEngine.Rendering;

public class CameraRenderer
{
    ScriptableRenderContext context;
    Camera camera;
    CullingResults cullingResults;

    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    const string kBufferName = "Render Camera";
    CommandBuffer buffer = new CommandBuffer { name = kBufferName };


    // This is for drawing unsupported shaders (optional)
    static Material errorMaterial;
    static ShaderTagId[] legacyShaderTagIds = {
        new ShaderTagId("Always"),
        new ShaderTagId("ForwardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM")
    };

    public void Render(ScriptableRenderContext context, Camera camera)
    {
        this.context = context;
        this.camera = camera;

        if (!Cull())
            return;

        Setup();
        DrawVisibleGeometry();
        DrawUnsupportedShaders();
        Submit();
    }

    bool Cull()
    {
        if (camera.TryGetCullingParameters(out var scriptableCullingParams))
        {
            cullingResults = context.Cull(ref scriptableCullingParams);
            return true;
        }

        return false;
    }

    void Setup()
    {
        context.SetupCameraProperties(camera);
        buffer.ClearRenderTarget(clearDepth: true, clearColor: true, Color.clear);
        buffer.BeginSample(kBufferName);
        ExecuteBuffer();
    }

    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    void DrawVisibleGeometry()
    {
        // A RendererList represents a set of draw commands that you send to a command buffer to execute
        // The order in which objects are rendered is important here it goes

        // Opaque objects first.
        var sortingSettings = new SortingSettings(camera);
        sortingSettings.criteria = SortingCriteria.CommonOpaque;
        var renderListParams = new RendererListParams()
        {
            cullingResults = this.cullingResults,
            drawSettings = new DrawingSettings(unlitShaderTagId, sortingSettings),
            filteringSettings = new FilteringSettings(RenderQueueRange.opaque)
        };

        var opaques = context.CreateRendererList(ref renderListParams);
        buffer.DrawRendererList(opaques);

        // Then the skybox
        var skyBox = context.CreateSkyboxRendererList(camera);
        buffer.DrawRendererList(skyBox);

        // Then the transparents
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        renderListParams.drawSettings.sortingSettings = sortingSettings;
        renderListParams.filteringSettings.renderQueueRange = RenderQueueRange.transparent;

        var transparents = context.CreateRendererList(ref renderListParams);
        buffer.DrawRendererList(transparents);
        
        // This is because scene objects are sorted back to front, which doesn't guarantee correct blending.
        // Intersecting & large transparent objects can still produce incorrect results, so breaking them up
        // into smaller geometries can sometimes solve it.
    }

    void DrawUnsupportedShaders()
    {
        if (errorMaterial == null)
            errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));

        var drawingSettings = new DrawingSettings(legacyShaderTagIds[0], new SortingSettings(camera));
        drawingSettings.overrideMaterial = errorMaterial;
        var renderListParams = new RendererListParams()
        {
            cullingResults = this.cullingResults,
            drawSettings = drawingSettings,
            filteringSettings = FilteringSettings.defaultValue
        };

        for (int i = 1; i < legacyShaderTagIds.Length; i++)
            renderListParams.drawSettings.SetShaderPassName(i, legacyShaderTagIds[i]);

        var unsupported = context.CreateRendererList(ref renderListParams);
        buffer.DrawRendererList(unsupported);
    }

    void Submit()
    {
        buffer.EndSample(kBufferName);
        ExecuteBuffer();
        context.Submit();
    }
}
