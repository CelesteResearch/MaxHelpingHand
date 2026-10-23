using System.Numerics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Triggers.Audio;
using Celeste.Mods.MaxHelpingHand.Components;

namespace Celeste.Mods.MaxHelpingHand.Triggers.Audio
{
    [CustomEntity("MaxHelpingHand/FlagToggleMusicFadeTrigger = Load")]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Triggers.FlagToggleMusicFadeTrigger")]
    public static class FlagToggleMusicFadeTrigger
    {
        public static InstalledMusicFadeTriggerState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
        {
            var trigger = new InstalledMusicFadeTriggerState(data, offset, level.EngineContext);
            trigger.OwnedComponents.Add(new FlagToggleComponent(data.Attr("flag"), data.Bool("inverted")));
            return trigger;
        }
    }
}
