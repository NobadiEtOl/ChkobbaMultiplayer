using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CRTPostProcessFeature : ScriptableRendererFeature
{
    class CRTPass : ScriptableRenderPass
    {
        public Material material;
        RenderTargetHandle tempTexture;

        public CRTPass(Material mat)
        {
            material = mat;
            tempTexture.Init("_TempCRTTexture");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (material == null)
            {
                Debug.LogWarning("CRT Material is null in CRTPass");
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get("CRTPass");

            // Get the camera color target within the render pass scope
            RenderTargetIdentifier source = renderingData.cameraData.renderer.cameraColorTarget;

            RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;
            cmd.GetTemporaryRT(tempTexture.id, desc);
            cmd.Blit(source, tempTexture.Identifier(), material);
            cmd.Blit(tempTexture.Identifier(), source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    public Material crtMaterial;
    CRTPass crtPass;

    public override void Create()
    {
        crtPass = new CRTPass(crtMaterial);
        crtPass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (crtMaterial == null) return;
        
        // Just enqueue the pass - camera target will be accessed within Execute()
        renderer.EnqueuePass(crtPass);
    }
}