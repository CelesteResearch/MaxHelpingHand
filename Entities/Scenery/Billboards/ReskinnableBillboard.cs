using System.Numerics;
using Celeste.Core.Components.Rendering;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Tracking.Attributes;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Entities.PlaybackBillboard;

namespace Celeste.Mods.MaxHelpingHand.Entities.Scenery.Billboards
{
    [CustomEntity("MaxHelpingHand/ReskinnableBillboard")]
    [EverestTrackedAs(typeof(PlaybackBillboardState))]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.ReskinnableBillboard")]
    public class ReskinnableBillboard : PlaybackBillboardState
    {
        private readonly ISourceEngineContext engine;
        private string borderTexture;

        public static ReskinnableBillboard Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(data, offset, level.EngineContext);

        public ReskinnableBillboard(EntityData data, Vector2 offset, ISourceEngineContext engine)
            : base(data, offset, engine)
        {
            this.engine = engine;
            borderTexture = EntityAttributeReader.Attr(data.Values, "borderTexture", "scenery/tvSlices");
            if (!EntityAttributeReader.Bool(data.Values, "renderBloom", true))
                Remove(Get<CustomBloomCallbackComponentState>());
        }

        internal string BorderTexture => borderTexture;
        internal bool MatchesEngine(ISourceEngineContext owner) => ReferenceEquals(engine, owner);
    }
}
