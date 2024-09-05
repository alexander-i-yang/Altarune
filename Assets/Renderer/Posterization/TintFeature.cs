using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class TintFeature : ScriptableRendererFeature
{
    class CustomRenderPass : ScriptableRenderPass
    {
        private Material material;
        private Material maskMat;
        private Material nonMat;
        private RenderStateBlock m_RenderStateBlock;

        private RenderTexture rt;

        private FilteringSettings filteringSettings;
        private RenderTargetIdentifier source;
        private RenderTargetHandle tempTexture;

        public CustomRenderPass(RenderTexture rt, Material material, Material maskMat, Material nonMat,
            LayerMask layerMask) : base()
        {
            this.rt = rt;
            this.material = material;
            this.maskMat = maskMat;
            this.nonMat = nonMat;
            filteringSettings = new FilteringSettings(RenderQueueRange.all, layerMask);

            m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

            tempTexture.Init("_TempTintTexture");
        }

        public void SetSource(RenderTargetIdentifier source)
        {
            this.source = source;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor cameraTextureDesc = renderingData.cameraData.cameraTargetDescriptor;
            cameraTextureDesc.depthBufferBits = 0;
            cmd.GetTemporaryRT(tempTexture.id, cameraTextureDesc, FilterMode.Point);
        }

        private readonly List<ShaderTagId> shaderTags = new List<ShaderTagId>
        {
            new ShaderTagId("SRPDefaultUnlit"),
            new ShaderTagId("UniversalForward"),
            new ShaderTagId("UniversalForwardOnly"),
            new ShaderTagId("LightweightForward")
        };

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("TintFeature");
            // flush buffer
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            if (rt != null)
            {
                cmd.SetRenderTarget(rt);
                // cmd.ClearRenderTarget(false, true, Color.black);
            }

            context.ExecuteCommandBuffer(
                cmd); // DrawRenderers doesn't respect buffer order so the above commands need to be done immediately
            cmd.Clear();

            var criteria = SortingCriteria.CommonOpaque;
            var drawingSettings = CreateDrawingSettings(shaderTags, ref renderingData, criteria);
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings,
                ref m_RenderStateBlock);

            // if (rt != null) cmd.SetRenderTarget(renderingData.cameraData.renderer.cameraColorTarget);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            /*CommandBuffer cmd = CommandBufferPool.Get("TintFeature");

            DrawingSettings drawSettings = CreateDrawingSettings(shaderTags, ref renderingData, SortingCriteria.CommonOpaque);
            drawSettings.overrideMaterial = maskMat;
            //DrawingSettings drawSettings2 = CreateDrawingSettings(shaderTags, ref renderingData, SortingCriteria.CommonOpaque);
            //context.DrawRenderers(renderingData.cullResults, ref drawSettings2, ref filteringSettings);
            context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filteringSettings);
            context.Submit();
            //Blit(cmd, source, tempTexture.Identifier(), material, 0);
            //Blit(cmd, tempTexture.Identifier(), source);

            //Blit(cmd, source, tempTexture.Identifier(), material, 0);
            //Blit(cmd, tempTexture.Identifier(), source);
            //Blit(cmd, ref renderingData, material, 0);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);*/
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(tempTexture.id);
        }
    }

    [System.Serializable]
    public class Settings
    {
        public RenderTexture rt;
        public Material material;
        public Material maskMat;
        public Material nonMat;
        public LayerMask layerMask;
    }

    [SerializeField] private Settings settings =
        new Settings();

    CustomRenderPass m_ScriptablePass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_ScriptablePass =
            new CustomRenderPass(settings.rt, settings.material, settings.maskMat, settings.nonMat, settings.layerMask);

        // Configures where the render pass should be injected.
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        // m_ScriptablePass.
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass); // letting the renderer know which passes will be used before allocation
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
    {
        m_ScriptablePass.SetSource(renderer.cameraColorTargetHandle); // use of target after allocation
    }
}