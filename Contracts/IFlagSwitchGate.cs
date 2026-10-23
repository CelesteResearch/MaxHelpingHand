// SPDX-License-Identifier: MIT

namespace Celeste.Mods.MaxHelpingHand.Contracts
{
    public interface IFlagSwitchGate
    {
        int ID { get; }
        string Flag { get; }
        bool Triggered { get; }
        void Trigger();
    }
}
