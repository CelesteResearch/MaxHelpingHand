using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.MaxHelpingHand.Modules;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.MaxHelpingHand.Triggers.Physics
{
    [CustomEntity("MaxHelpingHand/DisableIcePhysicsTrigger")]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Triggers.DisableIcePhysicsTrigger")]
    public class DisableIcePhysicsTrigger : SourceTriggerEntityState
    {
        private readonly MaxHelpingHandModule module;
        private bool disableIcePhysics;

        public static DisableIcePhysicsTrigger Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(level.EngineContext, data, offset);

        public DisableIcePhysicsTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
                data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            module = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<MaxHelpingHandModule>().Single();
            disableIcePhysics = EntityAttributeReader.Bool(data.Values, "disableIcePhysics", true);
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            base.OnEnter(player);
            module.Session.IcePhysicsDisabled = disableIcePhysics;
        }
    }
}
