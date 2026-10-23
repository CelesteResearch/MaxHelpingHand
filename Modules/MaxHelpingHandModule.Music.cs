using Celeste.Mods.MaxHelpingHand.Runtime.Music;

namespace Celeste.Mods.MaxHelpingHand.Modules
{
    public sealed partial class MaxHelpingHandModule
    {
        private void LoadMusicTriggers() => hooks.Add(new SpeedBasedMusicHooks(SourceEngine, () => Session));
    }
}
