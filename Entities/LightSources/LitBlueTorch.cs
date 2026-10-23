using System.Numerics;
using Celeste.Core.Maps;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Entities.LightSources.Torches;
using Celeste.Game.Execution.Levels;

namespace Celeste.Mods.MaxHelpingHand.Entities.LightSources
{
    [CustomEntity("MaxHelpingHand/LitBlueTorch")]
    [global::Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.LitBlueTorch")]
    public class LitBlueTorch : OriginalTorchState
    {
        public LitBlueTorch(ISourceEngineContext engine, OriginalLevelEntityData data, Vector2 offset, MapEntityId id)
            : base(engine, data, offset, id)
        {
        }

        public static LitBlueTorch Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) =>
            new LitBlueTorch(level.EngineContext, data, offset, new MapEntityId(room.Name, data.ID));

        public override void Added(object scene)
        {
            SourceStartLit = true;
            base.Added(scene);
        }
    }
}
