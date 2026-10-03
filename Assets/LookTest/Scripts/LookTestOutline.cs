using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Dungine.LookTest
{
    /// <summary>
    /// Look test: optional one-pixel outlines (Hidden/Dungine/Outline), added to the game camera from code each frame,
    /// so no renderer asset changes. Toggle with LookTestOutline.Set(true/false) from the dev tools.
    /// </summary>
    public static class LookTestOutline
    {
        static OutlinePass pass;
        static bool on;

        public static string Set(bool enable, float strength = 0.85f, float jump = 0.06f)
        {
            if (pass == null)
            {
                var sh = Shader.Find("Hidden/Dungine/Outline");
                if (!sh) return "outline shader not found";
                pass = new OutlinePass(new Material(sh)) { renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing };
            }
            pass.material.SetColor("_OutlineColor", new Color(0.03f, 0.03f, 0.04f, strength));
            pass.material.SetFloat("_OutlineJump", jump);
            if (enable && !on) RenderPipelineManager.beginCameraRendering += Enqueue;
            if (!enable && on) RenderPipelineManager.beginCameraRendering -= Enqueue;
            on = enable;
            return enable ? $"outlines on (strength {strength}, depth jump {jump})" : "outlines off";
        }

        static void Enqueue(ScriptableRenderContext ctx, Camera cam)
        {
            if (cam != Camera.main) return;
            var data = cam.GetUniversalAdditionalCameraData();
            if (data == null || data.scriptableRenderer == null) return;
            pass.ConfigureInput(ScriptableRenderPassInput.Depth);
            data.scriptableRenderer.EnqueuePass(pass);
        }

        class OutlinePass : ScriptableRenderPass
        {
            public readonly Material material;
            public OutlinePass(Material m) { material = m; }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                var res = frameData.Get<UniversalResourceData>();
                if (res.isActiveTargetBackBuffer) return;
                var src = res.activeColorTexture;
                var desc = renderGraph.GetTextureDesc(src);
                desc.name = "DungineOutline";
                desc.clearBuffer = false;
                desc.depthBufferBits = 0;
                var dst = renderGraph.CreateTexture(desc);
                renderGraph.AddBlitPass(new RenderGraphUtils.BlitMaterialParameters(src, dst, material, 0), "Dungine outline");
                res.cameraColor = dst;
            }
        }
    }
}
