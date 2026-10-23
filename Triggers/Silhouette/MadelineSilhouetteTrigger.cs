using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.MaxHelpingHand.Runtime.Silhouette;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.MaxHelpingHand.Triggers.Silhouette
{
    [CustomEntity("MaxHelpingHand/MadelineSilhouetteTrigger = Create")]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Triggers.MadelineSilhouetteTrigger")]
    public class MadelineSilhouetteTrigger : SourceTriggerEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly bool enable, revertOnLeave;
        private bool valueOnEnter;

        public static MadelineSilhouetteTrigger Create(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(level.EngineContext, data, offset);

        public MadelineSilhouetteTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
                data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            this.engine = engine;
            enable = data.Bool("enable", true);
            revertOnLeave = data.Bool("revertOnLeave", false);
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            base.OnEnter(player);
            var hooks = SilhouettePlayerHooks.For(engine);
            valueOnEnter = hooks.Session.MadelineIsSilhouette;
            hooks.Session.MadelineIsSilhouette = enable;
            if (valueOnEnter != enable) hooks.RefreshPlayerSpriteMode(player, enable);
        }

        public override void OnLeave(RoomPlayerSession player)
        {
            base.OnLeave(player);
            var hooks = SilhouettePlayerHooks.For(engine);
            if (revertOnLeave && valueOnEnter != hooks.Session.MadelineIsSilhouette)
            {
                hooks.Session.MadelineIsSilhouette = !enable;
                hooks.RefreshPlayerSpriteMode(player, !enable);
            }
        }
    }
}
