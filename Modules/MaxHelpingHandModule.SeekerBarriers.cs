using Celeste.Mods.MaxHelpingHand.Runtime.Barriers;

namespace Celeste.Mods.MaxHelpingHand.Modules
{
    public sealed partial class MaxHelpingHandModule
    {
        internal SeekerBarrierProcess SeekerBarriers { get; private set; }

        private void LoadSeekerBarriers()
        {
            SeekerBarriers = new SeekerBarrierProcess(SourceEngine, this);
            SeekerBarriers.Load();
            hooks.Add(SeekerBarriers);
        }
    }
}
