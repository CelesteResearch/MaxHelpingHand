using System;
using System.Numerics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Simulation.Presentation.Backdrops.Runtime;

namespace Celeste.Mods.MaxHelpingHand.Backdrops.Foundation
{
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Effects.CustomBackdrop")]
    public abstract class CustomBackdrop : BackdropPresentation
    {
        public float GetFadeAlpha(ISourceBackdropScene scene) => GetFadeAlphaFor(this, scene);

        public static float GetFadeAlphaFor(BackdropPresentation backdrop, ISourceBackdropScene scene)
        {
            float alpha = 1f;
            Vector2 value = scene.Camera.Position;
            value = new Vector2((float)Math.Floor(value.X), (float)Math.Floor(value.Y));
            if (backdrop.FadeX != null) alpha *= backdrop.FadeX.Value(value.X + 160f);
            if (backdrop.FadeY != null) alpha *= backdrop.FadeY.Value(value.Y + 90f);
            return alpha;
        }
    }
}
