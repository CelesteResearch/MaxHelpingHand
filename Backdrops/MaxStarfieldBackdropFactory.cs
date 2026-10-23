// SPDX-License-Identifier: MIT

using Celeste.Everest.Content.Maps.Binary;
using System;
using Celeste.Core.Display;
using Celeste.Graphics.Display.Colors;
using Celeste.Core.Scheduling;
using Celeste.Everest.Runtime.Levels.Update;
using Celeste.Formats.Map.Binary.Source;
using Celeste.Game.Content.Maps;
using Celeste.Mods.MaxHelpingHand.Backdrops.Auroras;
using Celeste.Mods.MaxHelpingHand.Backdrops.Stars;
using Celeste.Simulation.Presentation.Backdrops.Runtime;

namespace Celeste.Mods.MaxHelpingHand.Backdrops
{
    public sealed class MaxStarfieldBackdropFactory : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly NorthernLightsColorHooks northernLightsHooks;

        public MaxStarfieldBackdropFactory(ISourceEngineContext engine)
        {
            this.engine = engine;
            northernLightsHooks = new NorthernLightsColorHooks(engine);
            try
            {
                ((IEverestLevelEntityLoadEnvironment)engine).LevelUpdateEvents.OnLoadBackdrop += Load;
            }
            catch
            {
                northernLightsHooks.Dispose();
                throw;
            }
        }

        public BackdropPresentation Load(OriginalChapterMapData map, BinaryPackerElement child,
            BinaryPackerElement above)
        {
            if (child.Name.Equals("MaxHelpingHand/CustomStarfield", StringComparison.OrdinalIgnoreCase))
            {
                string[] paths = child.Attr("paths", "starfield").Split(',');
                var colors = Colors(child.Attr("colors", "ffffff"));
                string[] alphaStrings = child.Attr("alphas", "1").Split(',');
                var alphas = new float[alphaStrings.Length];
                for (int i = 0; i < alphas.Length; i++) alphas[i] = float.Parse(alphaStrings[i]);
                return new CustomStarfield(engine, paths, colors, alphas, child.AttrBool("shuffle", true),
                    child.AttrFloat("speed", 1f), child.AttrFloat("fps", 0f));
            }

            if (child.Name.Equals("MaxHelpingHand/NorthernLightsCustomColors", StringComparison.OrdinalIgnoreCase))
            {
                var config = new NorthernLightsColorConfiguration
                {
                    Engine = engine,
                    Colors = Colors(child.Attr("colors")),
                    GradientColor1 = child.Attr("gradientColor1", "020825"),
                    GradientColor2 = child.Attr("gradientColor2", "170c2f"),
                    ParticleCount = child.AttrInt("particleCount", 50),
                    StrandCount = child.AttrInt("strandCount", 3)
                };
                var previous = NorthernLightsColorHooks.Construction;
                NorthernLightsColorHooks.Construction = config;
                try
                {
                    return new NorthernLightsCustomColors(engine, config.Colors,
                        child.AttrBool("displayBackground", true));
                }
                finally
                {
                    NorthernLightsColorHooks.Construction = previous;
                }
            }

            return null;
        }

        private static PackedColor[] Colors(string input)
        {
            string[] values = input.Split(',');
            var result = new PackedColor[values.Length];
            for (int i = 0; i < values.Length; i++) result[i] = SourceHexColor.HexToColor(values[i]);
            return result;
        }

        public void Dispose()
        {
            ((IEverestLevelEntityLoadEnvironment)engine).LevelUpdateEvents.OnLoadBackdrop -= Load;
            northernLightsHooks.Dispose();
        }
    }
}
