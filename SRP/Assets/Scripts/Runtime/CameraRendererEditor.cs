using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

partial class CameraRenderer
{
    partial void DrawGizmos();
    partial void PrepareForSceneWindow();
    partial void DrawUnsupportedShaders();

#if UNITY_EDITOR
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

    partial void DrawGizmos()
    {
        if (Handles.ShouldRenderGizmos())
        {
			context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
			context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
		}
    }

    partial void PrepareForSceneWindow()
    {
        if (camera.cameraType == CameraType.SceneView)
            ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
    }

    partial void DrawUnsupportedShaders()
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
#endif
}
