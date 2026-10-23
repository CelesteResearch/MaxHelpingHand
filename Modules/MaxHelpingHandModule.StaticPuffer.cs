using Celeste.Mods.MaxHelpingHand.Gameplay.Hazards;

namespace Celeste.Mods.MaxHelpingHand.Modules
{
    public sealed partial class MaxHelpingHandModule
    {
        private void LoadStaticPuffers() => hooks.Add(new StaticPufferHooks(SourceEngine));
    }
}
