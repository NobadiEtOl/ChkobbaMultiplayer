using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CRTPostProcessFeature : ScriptableRendererFeature
{
    class CRTPass : ScriptableRenderPass
    {
        public Material CRTMaterial;
        private int tempTextureID = Shader.PropertyToID("_TempCRTTexture");

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (CRTMaterial == null) return;

            CommandBuffer cmd = CommandBufferPool.Get("CRTPass");
            
            // Get the camera color target as RenderTargetIdentifier
            RenderTargetIdentifier cameraColorTarget = renderingData.cameraData.renderer.cameraColorTargetHandle;
            RenderTextureDescriptor desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.depthBufferBits = 0;

            // Get temporary render texture
            cmd.GetTemporaryRT(tempTextureID, desc, FilterMode.Bilinear);

            // Apply CRT effect: Copy from camera target through CRT material to temp texture
            cmd.Blit(cameraColorTarget, tempTextureID, CRTMaterial);
            
            // Copy the result back to camera target
            cmd.Blit(tempTextureID, cameraColorTarget);

            // Clean up
            cmd.ReleaseTemporaryRT(tempTextureID);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    CRTPass crtPass;
    public Material CRTMaterial;
    

    public override void Create()
    {
        crtPass = new CRTPass();
        crtPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (CRTMaterial == null) return;
        
        crtPass.CRTMaterial = CRTMaterial;
        renderer.EnqueuePass(crtPass);
    }
}