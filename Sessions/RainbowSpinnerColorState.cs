using System.Collections.Generic;
using System.Numerics;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Mods.MaxHelpingHand.Runtime.Spinners.Rainbow;

namespace Celeste.Mods.MaxHelpingHand.Sessions
{
    public sealed class RainbowSpinnerColorState
    {
        public string Colors { get; set; }
        public float GradientSize { get; set; }
        public bool LoopColors { get; set; }
        public Vector2 Center { get; set; }
        public float GradientSpeed { get; set; }
        public string Flag { get; set; }
        public string ColorsWithFlag { get; set; }
        public float GradientSizeWithFlag { get; set; }
        public bool LoopColorsWithFlag { get; set; }
        public Vector2 CenterWithFlag { get; set; }
        public float GradientSpeedWithFlag { get; set; }

        internal static RainbowSpinnerColorState FromData(EntityData data, RainbowGradientSettings normal,
            RainbowGradientSettings flagged) => new()
        {
            Colors = data.Attr("colors", RainbowGradientSettings.DefaultColors), GradientSize = normal.GradientSize,
            LoopColors = normal.LoopColors, Center = normal.Center, GradientSpeed = normal.GradientSpeed,
            Flag = data.Attr("flag"),
            ColorsWithFlag = data.Attr("colorsWithFlag", RainbowGradientSettings.DefaultColors),
            GradientSizeWithFlag = flagged.GradientSize, LoopColorsWithFlag = flagged.LoopColors,
            CenterWithFlag = flagged.Center, GradientSpeedWithFlag = flagged.GradientSpeed
        };

        internal EntityData Restore() => new()
        {
            Values = new Dictionary<string, object>
            {
                ["colors"] = Colors, ["gradientSize"] = GradientSize, ["loopColors"] = LoopColors,
                ["centerX"] = Center.X, ["centerY"] = Center.Y, ["gradientSpeed"] = GradientSpeed,
                ["flag"] = Flag, ["colorsWithFlag"] = ColorsWithFlag, ["gradientSizeWithFlag"] = GradientSizeWithFlag,
                ["loopColorsWithFlag"] = LoopColorsWithFlag, ["centerXWithFlag"] = CenterWithFlag.X,
                ["centerYWithFlag"] = CenterWithFlag.Y, ["gradientSpeedWithFlag"] = GradientSpeedWithFlag,
                ["persistent"] = true
            }
        };
    }
}
