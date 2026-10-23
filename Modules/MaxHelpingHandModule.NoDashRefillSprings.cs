using Celeste.Mods.MaxHelpingHand.Runtime.Springs;

namespace Celeste.Mods.MaxHelpingHand.Modules
{
    public sealed partial class MaxHelpingHandModule
    {
        private void LoadNoDashRefillSprings() => hooks.Add(new NoDashRefillSpringLightingHooks(SourceEngine));
    }
}
