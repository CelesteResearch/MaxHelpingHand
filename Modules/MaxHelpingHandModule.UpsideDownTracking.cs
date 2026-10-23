using Celeste.Mods.MaxHelpingHand.Collision;

namespace Celeste.Mods.MaxHelpingHand.Modules
{
    public sealed partial class MaxHelpingHandModule
    {
        private UpsideDownTrackerProcess upsideDownTracker;
        private UpsideDownPlayerHooks upsideDownPlayer;

        private void LoadUpsideDownTracking()
        {
            upsideDownTracker = new UpsideDownTrackerProcess();
            upsideDownPlayer = new UpsideDownPlayerHooks(SourceEngine);
        }

        private void UnloadUpsideDownTracking()
        {
            upsideDownPlayer?.Dispose();
            upsideDownPlayer = null;
            upsideDownTracker?.Dispose();
            upsideDownTracker = null;
        }
    }
}
