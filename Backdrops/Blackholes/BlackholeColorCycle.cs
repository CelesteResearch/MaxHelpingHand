using System;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Graphics.Display.Colors;

namespace Celeste.Mods.MaxHelpingHand.Backdrops.Blackholes
{
    internal sealed class BlackholeColorCycle
    {
        private readonly PackedColor[][] colors;
        private readonly float cycle;
        private int cycleProgress;
        private float cycleTimer;
        private readonly ISourceScalarArithmetic arithmetic;

        public BlackholeColorCycle(string source, float alpha, ISourceScalarArithmetic arithmetic)
        {
            this.arithmetic = arithmetic;
            if (!source.Contains("|")) colors = new[] { ParseColors(source) };
            else
            {
                string[] sources = source.Split('|');
                cycle = float.Parse(sources[0]);
                colors = new PackedColor[sources.Length - 1][];
                for (int i = 1; i < sources.Length; i++) colors[i - 1] = ParseColors(sources[i]);
            }

            for (int i = 0; i < colors.Length; i++)
            for (int j = 0; j < colors[0].Length; j++)
                colors[i][j] *= alpha;
        }

        public void Update(float deltaTime)
        {
            if (colors.Length == 1) return;
            cycleTimer += deltaTime;
            if (cycleTimer >= cycle)
            {
                cycleTimer -= cycle;
                cycleProgress++;
                cycleProgress %= colors.Length;
            }
        }

        public PackedColor[] GetColors()
        {
            if (colors.Length == 1) return colors[0];
            var from = colors[cycleProgress];
            var to = colors[(cycleProgress + 1) % colors.Length];
            var result = new PackedColor[from.Length];
            for (int i = 0; i < result.Length; i++)
                result[i] = SourceColorInterpolation.Lerp(from[i], to[i], cycleTimer / cycle, arithmetic);
            return result;
        }

        internal static PackedColor[] ParseColors(string input)
        {
            string[] strings = input.Split(',');
            var result = new PackedColor[strings.Length];
            for (int i = 0; i < result.Length; i++) result[i] = SourceHexColor.HexToColor(strings[i]);
            return result;
        }
    }
}
