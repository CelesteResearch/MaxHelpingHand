using System;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Scheduling;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Graphics.Display.Colors;
using Celeste.Scene.Tracking;

namespace Celeste.Mods.MaxHelpingHand.Runtime.Spinners.Rainbow
{
    public sealed class RainbowGradientSettings
    {
        public PackedColor[] Colors { get; }
        public float GradientSize { get; }
        public bool LoopColors { get; }
        public Vector2 Center { get; }
        public float GradientSpeed { get; }
        public const string DefaultColors = "89E5AE,88E0E0,87A9DD,9887DB,D088E2";

        public RainbowGradientSettings(EntityData data, bool withFlag)
        {
            var suffix = withFlag ? "WithFlag" : "";
            GradientSize = data.Float("gradientSize" + suffix, 280);
            LoopColors = data.Bool("loopColors" + suffix);
            Center = new Vector2(data.Float("centerX" + suffix), data.Float("centerY" + suffix));
            GradientSpeed = data.Float("gradientSpeed" + suffix, 50);
            var values = data.Attr("colors" + suffix, DefaultColors).Split(',');
            Colors = new PackedColor[values.Length + (LoopColors ? 1 : 0)];
            for (var i = 0; i < values.Length; i++) Colors[i] = SourceHexColor.HexToColor(values[i]);
            if (LoopColors) Colors[^1] = Colors[0];
        }

        public PackedColor GetHue(object scene, Vector2 position)
        {
            if (Colors.Length == 1) return Colors[0];
            var progress =
                ((ISourceVectorEnvironment)((ISourceEngineContextScene)scene).EngineContext).VectorArithmetic.Length(
                    position - Center) + ((ISourceScene)scene).SourceScene.Clock.TimeActive * GradientSpeed;
            while (progress < 0) progress += GradientSize;
            progress = progress % GradientSize / GradientSize;
            if (!LoopColors) progress = ScalarMath.YoYo(progress);
            if (progress == 1) return Colors[^1];
            var globalProgress = (Colors.Length - 1) * progress;
            var index = (int)globalProgress;
            return PackedColor.Lerp(Colors[index], Colors[index + 1], globalProgress - index);
        }
    }
}
