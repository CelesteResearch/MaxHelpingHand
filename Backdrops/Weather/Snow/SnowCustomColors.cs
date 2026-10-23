using System;
using System.Numerics;
using Celeste.Graphics.Geometry.TwoD.Camera;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Graphics.Rendering;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Mods.MaxHelpingHand.Modules;
using Celeste.Simulation.Presentation.Backdrops.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Content.Maps.Binary;
using Celeste.Formats.Map.Binary.Source;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Display.Colors;
using Celeste.Graphics.Rendering.Drawing;
using Celeste.Simulation.Presentation.Backdrops.Weather.Snow;

namespace Celeste.Mods.MaxHelpingHand.Backdrops.Weather.Snow
{
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Effects.SnowCustomColors")]
    public class SnowCustomColors : SnowBackdropPresentation
    {
        private readonly MaxHelpingHandModule owner;
        private readonly ISourceEngineContext engine;
        private readonly Vector2 direction, perpendicular;
        private readonly float sineAmplitude;
        private AtlasEntry particleTexture;

        public SnowCustomColors(ISourceEngineContext engine, PackedColor[] colors, float speedMin, float speedMax,
            int particleCount, float angle, float sineAmplitudeMult, string texturePath)
            : this(engine, colors, speedMin, speedMax, particleCount, angle, sineAmplitudeMult, texturePath, null)
        {
        }

        private SnowCustomColors(ISourceEngineContext engine, PackedColor[] colors, float speedMin, float speedMax,
            int particleCount, float angle, float sineAmplitudeMult, string texturePath, MaxHelpingHandModule owner)
            : base(engine, false)
        {
            this.owner = owner;
            this.engine = engine;
            SnowFieldAccess.Configure(this, colors, particleCount, speedMin, speedMax);
            direction = Vector2Geometry.AngleToVector(angle, 1f);
            perpendicular = -Vector2Geometry.Perpendicular(direction);
            sineAmplitude = sineAmplitudeMult;
            particleTexture = string.IsNullOrWhiteSpace(texturePath)
                ? ((ISourceDrawScope)engine).Draw.Pixel
                : ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas[texturePath];
        }

        public override void Update(ISourceBackdropScene scene)
        {
            SnowFieldAccess.UpdateBackdrop(this, scene);
            SnowFieldAccess.SetVisibleFade(this, ScalarMath.Approach(SnowFieldAccess.VisibleFade(this),
                IsVisible(scene) ? 1f : 0f,
                engine.Frame.DeltaTime / .5f, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic));
            SnowFieldAccess.SetLinearFade(this, 1f);
            if (FadeX != null)
                SnowFieldAccess.SetLinearFade(this,
                    SnowFieldAccess.LinearFade(this) * FadeX.Value(scene.Camera.X + scene.Camera.Viewport.Width / 2f));
            if (FadeY != null)
                SnowFieldAccess.SetLinearFade(this,
                    SnowFieldAccess.LinearFade(this) * FadeY.Value(scene.Camera.Y + scene.Camera.Viewport.Height / 2f));
            SnowFieldAccess.UpdateParticles(this, direction, perpendicular, sineAmplitude, engine);
        }

        public override void Render(ISourceRenderContext context, ISourceBackdropScene scene)
        {
            float visibleFade = SnowFieldAccess.VisibleFade(this), linearFade = SnowFieldAccess.LinearFade(this);
            if (Alpha <= 0f || visibleFade <= 0f || linearFade <= 0f) return;
            PackedColor[] colors = SnowFieldAccess.Colors(this), blended = SnowFieldAccess.BlendedColors(this);
            for (int i = 0; i < blended.Length; i++) blended[i] = colors[i] * (Alpha * visibleFade * linearFade);
            var camera = scene.Camera;
            Vector2 padding = new(particleTexture.FrameWidth / 2f - .5f, particleTexture.FrameHeight / 2f - .5f);
            SnowFieldAccess.RenderParticles(this, context, camera, particleTexture, padding,
                owner?.SnowTileToViewport ?? false);
        }

        public static SnowCustomColors Create(ISourceEngineContext engine, BinaryPackerElement child,
            MaxHelpingHandModule owner = null)
        {
            float alpha = child.ReadInstalledFloat("alpha", 1f);
            string[] entries = child.ReadInstalledString("colors", "").Split(',');
            var colors = new PackedColor[entries.Length];
            for (int i = 0; i < colors.Length; i++) colors[i] = SourceHexColor.HexToColor(entries[i]) * alpha;
            return new SnowCustomColors(engine, colors, child.ReadInstalledFloat("speedMin", 40f),
                child.ReadInstalledFloat("speedMax", 100f), child.ReadInstalledInt("particleCount", 60),
                child.ReadInstalledFloat("angle", 180f) * ((float)System.Math.PI / 180f),
                child.ReadInstalledFloat("sineAmplitudeMult", .2f), child.ReadInstalledString("texturePath", ""),
                owner);
        }
    }
}
