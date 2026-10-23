using Celeste.Mods.MaxHelpingHand.Runtime.Silhouette;

namespace Celeste.Mods.MaxHelpingHand.Modules
{
    public sealed partial class MaxHelpingHandModule
    {
        private void LoadSilhouette() => hooks.Add(new SilhouettePlayerHooks(SourceEngine, () => Session));
    }
}
