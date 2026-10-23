// SPDX-License-Identifier: MIT

using Celeste.Core.Display;
using Celeste.Core.Scheduling;

namespace Celeste.Mods.MaxHelpingHand.Backdrops.Auroras
{
    internal sealed class NorthernLightsColorConfiguration
    {
        internal ISourceEngineContext Engine;
        internal PackedColor[] Colors;
        internal string GradientColor1, GradientColor2;
        internal int ParticleCount, StrandCount;
    }
}
