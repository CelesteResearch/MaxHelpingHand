// SPDX-License-Identifier: MIT

using Celeste.Mods.MaxHelpingHand.Runtime.FlagSwitches;

namespace Celeste.Mods.MaxHelpingHand.Modules
{
    public sealed partial class MaxHelpingHandModule
    {
        internal MovingFlagTouchSwitchProcess MovingFlagTouchSwitches { get; private set; }

        private void LoadMovingFlagTouchSwitches()
        {
            MovingFlagTouchSwitches = new MovingFlagTouchSwitchProcess(SourceEngine, FlagSwitchGroups);
            hooks.Add(MovingFlagTouchSwitches);
        }
    }
}
