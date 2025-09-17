using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CRTFullScreenFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class CRTSettings
    {
        public Material crtMaterial;
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    public CRTSettings settings = new CRTSettings();

    class CRTFullScreenPass : ScriptableRenderPass
    {
        private Material material;
        private RenderTargetHandle tempTexture;

        public CRTFullScreenPass(Material mat, RenderPassEvent passEvent)
        {
            material = mat;
            renderPassEvent = passEvent;
            tempTexture.Init("_TempCRTTexture");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (material == null)
            {
                Debug.LogWarning("CRT Material is null in CRTFullScreenPass");
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get("CRT FullScreen Pass");

            // Get the camera color target within the render pass scope
            RenderTargetIdentifier source = renderingData.cameraData.renderer.cameraColorTarget;
            
            RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            
            // Mobile optimization: reduce resolution for better performance
            #if UNITY_ANDROID || UNITY_IOS
            if (desc.width > 1920)
            {
                desc.width = 1920;
                desc.height = (int)(desc.height * (1920.0f / desc.width));
            }
            #endif

            cmd.GetTemporaryRT(tempTexture.id, desc, FilterMode.Bilinear);
            cmd.Blit(source, tempTexture.Identifier(), material);
            cmd.Blit(tempTexture.Identifier(), source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            if (tempTexture != RenderTargetHandle.CameraTarget)
            {
                cmd.ReleaseTemporaryRT(tempTexture.id);
            }
        }
    }

    CRTFullScreenPass crtPass;

    public override void Create()
    {
        if (settings.crtMaterial == null)
        {
            Debug.LogWarning("CRT Material not assigned.");
            return;
        }

        crtPass = new CRTFullScreenPass(settings.crtMaterial, settings.renderPassEvent);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (settings.crtMaterial == null) return;
        
        // Just enqueue the pass - camera target will be accessed within Execute()
        renderer.EnqueuePass(crtPass);
    }
}