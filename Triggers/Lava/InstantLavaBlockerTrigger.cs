using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Entities.Triggers.Lava;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;

namespace Celeste.Mods.MaxHelpingHand.Triggers.Lava
{
    [CustomEntity("MaxHelpingHand/InstantLavaBlockerTrigger")]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Triggers.InstantLavaBlockerTrigger")]
    [SceneTracked(false)]
    public sealed class InstantLavaBlockerTrigger : EverestLavaBlockerTriggerState
    {
        private readonly bool canReenter;

        public InstantLavaBlockerTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(engine,
            data, offset)
        {
            canReenter = data.Bool("canReenter", false);
        }

        public override void OnLeave(RoomPlayerSession player)
        {
            base.OnLeave(player);
            if (!canReenter) RemoveSelf();
        }
    }
}
