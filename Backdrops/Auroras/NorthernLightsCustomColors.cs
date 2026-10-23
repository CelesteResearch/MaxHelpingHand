// SPDX-License-Identifier: MIT

using System;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Game.Presentation.Backdrops.NorthernLights;
using Celeste.Simulation.Presentation.Backdrops.Runtime;

namespace Celeste.Mods.MaxHelpingHand.Backdrops.Auroras
{
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Effects.NorthernLightsCustomColors")]
    public class NorthernLightsCustomColors : NorthernLightsBackdropPresentation
    {
        internal readonly PackedColor[] CustomColors;
        internal readonly bool DisplayBackground;
        internal readonly ISourceEngineContext Engine;

        public NorthernLightsCustomColors(ISourceEngineContext engine, PackedColor[] colors, bool displayBackground)
            : base(engine)
        {
            Engine = engine;
            CustomColors = colors;
            DisplayBackground = displayBackground;
        }

        public override void Update(ISourceBackdropScene scene)
        {
            var previous = NorthernLightsColorHooks.ActiveColors;
            NorthernLightsColorHooks.ActiveColors = CustomColors;
            try
            {
                base.Update(scene);
            }
            finally
            {
                NorthernLightsColorHooks.ActiveColors = previous;
            }
        }
    }
}
