using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ToonPostProcessFeature : ScriptableRendererFeature
{
    class ToonPass : ScriptableRenderPass
    {
        private Material _material;
        private RTHandle _tempTexture;
        private const string TempTextureName = "_TempToonTexture";

        public ToonPass(Material mat)
        {
            _material = mat;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref _tempTexture, desc, name: TempTextureName);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null)
                return;

            var cmd = CommandBufferPool.Get("ToonPostProcess");

            RTHandle source = renderingData.cameraData.renderer.cameraColorTargetHandle;

            Blitter.BlitCameraTexture(cmd, source, _tempTexture, _material, 0);
            Blitter.BlitCameraTexture(cmd, _tempTexture, source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // RTHandle is managed by ReAllocateIfNeeded; no manual release needed here
        }

        public void Dispose()
        {
            _tempTexture?.Release();
        }
    }

    [System.Serializable]
    public class ToonSettings
    {
        public Material material = null;
    }

    public ToonSettings settings = new ToonSettings();
    private ToonPass _toonPass;

    public override void Create()
    {
        if (settings.material != null)
        {
            _toonPass = new ToonPass(settings.material)
            {
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing
            };
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_toonPass != null)
        {
            renderer.EnqueuePass(_toonPass);
        }
    }

    protected override void Dispose(bool disposing)
    {
        _toonPass?.Dispose();
    }
}