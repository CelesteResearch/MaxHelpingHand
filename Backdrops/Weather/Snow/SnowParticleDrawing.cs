using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Rendering;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Display;
using Celeste.Graphics.Geometry.TwoD.Camera;
using Celeste.Simulation.Presentation.Backdrops.Weather.Snow;

namespace Celeste.Mods.MaxHelpingHand.Backdrops.Weather.Snow
{
    internal static class SnowParticleDrawing
    {
        internal static void RenderParticle(SnowBackdropPresentation snow, ISourceRenderContext context,
            CameraState camera, AtlasEntry texture, Vector2 padding, bool tiled, Vector2 particlePosition,
            PackedColor color)
        {
            Vector2 position = new(
                ScalarMath.Mod(particlePosition.X - camera.X * snow.Scroll.X, 320f + padding.X * 2f) - padding.X,
                ScalarMath.Mod(particlePosition.Y - camera.Y * snow.Scroll.Y, 180f + padding.Y * 2f) - padding.Y);
            if (!tiled)
            {
                DrawParticle(context, texture, position, color);
                return;
            }

            for (float x = position.X; x < camera.Viewport.Width + padding.X; x += 320f + padding.X * 2f)
            for (float y = position.Y; y < camera.Viewport.Height + padding.Y; y += 180f + padding.Y * 2f)
                DrawParticle(context, texture, new Vector2(x, y), color);
        }

        private static void DrawParticle(ISourceRenderContext context, AtlasEntry texture, Vector2 position,
            PackedColor color)
        {
            var draw = new AtlasTextureDraw(texture, position, texture.Center, color, Vector2.One);
            ((ISourceRenderCommandSink<AtlasTextureDraw>)context).Draw(in draw, null);
        }
    }
}
