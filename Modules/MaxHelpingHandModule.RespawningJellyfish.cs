using Celeste.Mods.MaxHelpingHand.Runtime.Holdables.Gliders;

namespace Celeste.Mods.MaxHelpingHand.Modules
{
    public sealed partial class MaxHelpingHandModule
    {
        private void LoadRespawningJellyfish() => hooks.Add(new RespawningJellyfishProcess(SourceEngine));
    }
}
