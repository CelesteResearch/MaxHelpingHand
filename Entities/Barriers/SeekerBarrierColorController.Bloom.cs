using System.Numerics;
using Celeste.Core.Components.Rendering;
using Celeste.Core.Display;
using Celeste.Core.Rendering.States;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Presentation.Effects.Bloom;
using Celeste.Game.Presentation.Rendering;
using Celeste.Graphics.Effects.Blur;
using Celeste.Graphics.Rendering;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Enemies.Seeker;
using Celeste.Simulation.Presentation.Lighting;

namespace Celeste.Mods.MaxHelpingHand.Entities.Barriers
{
    public sealed partial class SeekerBarrierColorController
    {
        internal void RenderDepthBloom(OriginalLevelSceneRoot level)
        {
            var graphics = (IOriginalBloomProcess)engine;
            var buffers = graphics.Graphics.Buffers;
            var batch = graphics.Draw.SpriteBatch;
            GameplayRendererState.End(level.SourceRenderContext);
            EnsureBuffer();
            graphics.GraphicsDevice.SetRenderTarget(levelRenderTarget.Target);
            graphics.GraphicsDevice.Clear(PackedColor.Transparent);
            level.Background.Render(level);
            batch.Begin(SourceSpriteSortMode.Deferred, SourceBlendState.AlphaBlend, SourceSamplerState.PointClamp,
                SourceDepthStencilState.None, SourceRasterizerState.CullNone);
            batch.Draw(buffers.Gameplay.Target, Vector2.Zero, PackedColor.White.PackedValue);
            batch.End();
            var blurred =
                GaussianBlurPresentation.Blur(graphics, levelRenderTarget.Target, buffers.TempA, buffers.TempB);
            graphics.GraphicsDevice.SetRenderTarget(buffers.TempA.Target);
            graphics.GraphicsDevice.Clear(PackedColor.Transparent);
            var camera = level.Camera;
            batch.Begin(SourceSpriteSortMode.Deferred, SourceBlendState.AlphaBlend, SourceSamplerState.PointClamp,
                SourceDepthStencilState.None, SourceRasterizerState.CullNone, null, camera.Matrix);
            process.AllowedToRenderBloom = true;
            foreach (CustomBloomCallbackComponentState bloom in level.SourceScene.Components.GetTrackedComponents(
                         "Celeste.CustomBloom"))
                if (bloom.Visible && bloom.OnRenderBloom != null && bloom.Entity is SeekerBarrierRendererState)
                    bloom.OnRenderBloom();
            process.AllowedToRenderBloom = false;
            batch.End();
            var cutouts = level.SourceScene.Components.GetTrackedComponents("Celeste.EffectCutout");
            if (cutouts.Count > 0)
            {
                batch.Begin(SourceSpriteSortMode.Deferred, graphics.BloomBlendStates.CutoutBlendstate,
                    SourceSamplerState.PointClamp, null, null, null, camera.Matrix);
                foreach (EffectCutoutState cutout in cutouts)
                    if (cutout.Visible)
                        SourcePrimitiveDrawing.Rect(graphics, cutout.Left, cutout.Top,
                            cutout.Right - cutout.Left, cutout.Bottom - cutout.Top,
                            PackedColor.White * (1f - cutout.Alpha));
                batch.End();
            }

            batch.Begin(SourceSpriteSortMode.Deferred, graphics.BloomBlendStates.BlurredScreenToMask);
            batch.Draw(blurred, Vector2.Zero, PackedColor.White.PackedValue);
            batch.End();
            graphics.GraphicsDevice.SetRenderTarget(buffers.Gameplay.Target);
            batch.Begin(SourceSpriteSortMode.Deferred, graphics.BloomBlendStates.AdditiveMaskToScreen);
            batch.Draw(buffers.TempA.Target, Vector2.Zero, PackedColor.White.PackedValue);
            batch.End();
            GameplayRendererState.Begin(level.SourceRenderContext);
        }
    }
}
