using Celeste.Mods.MaxHelpingHand.Runtime.Refills;

namespace Celeste.Mods.MaxHelpingHand.Modules
{
    public sealed partial class MaxHelpingHandModule
    {
        private void LoadCustomizableRefills() => hooks.Add(new CustomizableRefillHooks(SourceEngine));
    }
}
