using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class FogRendererFeature : ScriptableRendererFeature
{
    class FogPass : ScriptableRenderPass
    {
        private Material material;
        private RTHandle tempTexture;
        private RTHandle source;

        public FogPass(Material mat)
        {
            material = mat;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            source = renderingData.cameraData.renderer.cameraColorTargetHandle;

            var desc = renderingData.cameraData.cameraTargetDescriptor;
            RenderingUtils.ReAllocateIfNeeded(ref tempTexture, desc, name: "_TempFogTexture");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (material == null || source == null || tempTexture == null)
                return;

            var stack = VolumeManager.instance.stack;
            var fog = stack.GetComponent<Fog>();
            if (fog == null || !fog.IsActive())
                return;

            material.SetColor("_FogColor", fog.fogColor.value);
            material.SetFloat("_Density", fog.density.value);
            material.SetFloat("_Start", fog.start.value);
            material.SetFloat("_End", fog.end.value);
            material.SetFloat("_Height", fog.height.value);
            material.SetFloat("_HeightDensity", fog.heightDensity.value);
            material.SetFloat("_ExcludeSkybox", fog.excludeSkybox.value ? 1f : 0f);

            var cmd = CommandBufferPool.Get("URP Fog");

            Blitter.BlitCameraTexture(cmd, source, tempTexture, material, 0);
            Blitter.BlitCameraTexture(cmd, tempTexture, source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
    }

    public Shader fogShader;
    private Material fogMaterial;
    private FogPass fogPass;

    public override void Create()
    {
        if (fogShader == null)
            fogShader = Shader.Find("Hidden/URP/Fog");

        fogMaterial = CoreUtils.CreateEngineMaterial(fogShader);
        fogPass = new FogPass(fogMaterial);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType != CameraType.Game && renderingData.cameraData.cameraType != CameraType.SceneView)
            return;

        renderer.EnqueuePass(fogPass);
    }
}
