using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Presentation.Backdrops.Blackhole;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Rendering.Textures;
using Celeste.Simulation.Presentation.Backdrops.Runtime;

namespace Celeste.Mods.MaxHelpingHand.Backdrops.Blackholes
{
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Effects.BlackholeCustomColors")]
    public partial class BlackholeCustomColors : BlackholeBackdropPresentation
    {
        private readonly ISourceEngineContext engine;
        private readonly BlackholeColorCycle cycleColorsMild;
        private readonly BlackholeColorCycle cycleColorsWild;
        private readonly BlackholeColorCycle cycleBgColorInner;
        private readonly BlackholeColorCycle cycleBgColorOuterMild;
        private readonly BlackholeColorCycle cycleBgColorOuterWild;
        private readonly bool affectedByWind;
        private readonly Vector2 additionalWind;
        private readonly float fgAlpha;
        private readonly bool invertedRendering;
        private readonly AtlasEntry particleTexture;

        private BlackholeCustomColors(ISourceEngineContext engine, BlackholeBackdropConstruction construction,
            string colorsMild, string colorsWild, string inner, string outerMild, string outerWild,
            float alphaInner, float alphaOuter, float fgAlpha, bool affectedByWind,
            Vector2 additionalWind, bool invertedRendering, AtlasEntry particleTexture)
            : base(engine, construction)
        {
            this.engine = engine;
            var arithmetic = ((ISourceScalarEnvironment)engine).ScalarArithmetic;
            cycleColorsMild = new BlackholeColorCycle(colorsMild, .8f * fgAlpha, arithmetic);
            cycleColorsWild = new BlackholeColorCycle(colorsWild, fgAlpha, arithmetic);
            cycleBgColorInner = new BlackholeColorCycle(inner, alphaInner, arithmetic);
            cycleBgColorOuterMild = new BlackholeColorCycle(outerMild, alphaOuter, arithmetic);
            cycleBgColorOuterWild = new BlackholeColorCycle(outerWild, alphaOuter, arithmetic);
            this.affectedByWind = affectedByWind;
            this.additionalWind = additionalWind;
            this.particleTexture = particleTexture;
            this.fgAlpha = fgAlpha;
            this.invertedRendering = invertedRendering;
            if (invertedRendering)
                SetBackgroundTexture(((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas[
                    "objects/MaxHelpingHand/temple/portal_inverted"]);
        }

        public override void Update(ISourceBackdropScene scene)
        {
            if (affectedByWind && additionalWind == Vector2.Zero) base.Update(scene);
            else
            {
                var level = (OriginalLevelSceneRoot)scene;
                Vector2 wind = level.Wind;
                if (!affectedByWind) level.Wind = Vector2.Zero;
                level.Wind += additionalWind;
                base.Update(scene);
                level.Wind = wind;
            }

            cycleColorsMild.Update(engine.DeltaTime);
            cycleColorsWild.Update(engine.DeltaTime);
            cycleBgColorInner.Update(engine.DeltaTime);
            cycleBgColorOuterMild.Update(engine.DeltaTime);
            cycleBgColorOuterWild.Update(engine.DeltaTime);
            ApplyColors(new BlackholeBackdropPalette(cycleColorsMild.GetColors(), cycleColorsWild.GetColors(),
                cycleBgColorInner.GetColors()[0], cycleBgColorOuterMild.GetColors()[0],
                cycleBgColorOuterWild.GetColors()[0]));
        }

        public override void Render(ISourceRenderContext context, ISourceBackdropScene scene)
        {
            float alpha = Alpha;
            float fade = 1f;
            Vector2 camera = Vector2Geometry.Floor(scene.Camera.Position);
            if (FadeX != null) fade *= FadeX.Value(camera.X + 160f);
            if (FadeY != null) fade *= FadeY.Value(camera.Y + 90f);
            Alpha *= fade;
            base.Render(context, scene);
            Alpha = alpha;
        }

        protected override PackedColor ForegroundBlack => PackedColor.Black * fgAlpha;

        protected override PackedColor BackgroundClearColor
            => invertedRendering ? PackedColor.Transparent : base.BackgroundClearColor;

        protected override int BackgroundFirstLayer => invertedRendering ? 19 : base.BackgroundFirstLayer;
        protected override int BackgroundLayerStep => invertedRendering ? -1 : base.BackgroundLayerStep;

        protected override int BackgroundLayerIndex(int layer)
            => invertedRendering ? 19 - layer : base.BackgroundLayerIndex(layer);

        protected override SourceTexture2DResource ResolvePrimitiveTexture(SourceTexture2DResource texture)
            => particleTexture != null ? particleTexture.Texture.Texture : texture;
    }
}
