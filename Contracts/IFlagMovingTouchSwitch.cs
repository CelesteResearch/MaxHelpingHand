// SPDX-License-Identifier: MIT

namespace Celeste.Mods.MaxHelpingHand.Contracts
{
    public interface IFlagMovingTouchSwitch
    {
        string Flag { get; }
        bool Activated { get; }
        bool Hidden { get; }
        void Finish();
    }
}
