using Celeste.Mods.MaxHelpingHand.Runtime.Barriers;

namespace Celeste.Mods.MaxHelpingHand.Modules
{
    public sealed partial class MaxHelpingHandModule
    {
        private KevinBarrierHooks kevinBarrierHooks;

        public override void LoadContent(bool firstLoad)
        {
            global::Celeste.Mods.MaxHelpingHand.Entities.Collectibles.Strawberries.MultiRoom.MultiRoomStrawberryCounter
                .Initialize(SourceEngine);
            instantLavaBlockerHooks?.HookMods();
            kevinBarrierHooks?.HookMods();
            MovingFlagTouchSwitches?.HookMods();
        }

        private void LoadKevinBarriers() => hooks.Add(kevinBarrierHooks = new KevinBarrierHooks(SourceEngine));
    }
}
