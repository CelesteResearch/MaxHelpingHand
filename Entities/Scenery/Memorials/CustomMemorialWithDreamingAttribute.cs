using System.Numerics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Entities.Memorials;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Entities;

namespace Celeste.Mods.MaxHelpingHand.Entities.Scenery.Memorials
{
    [CustomEntity("MaxHelpingHand/CustomMemorialWithDreamingAttribute")]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.CustomMemorialWithDreamingAttribute")]
    public class CustomMemorialWithDreamingAttribute : CustomMemorialState
    {
        private bool dreaming;

        public CustomMemorialWithDreamingAttribute(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(
            data, offset, engine)
        {
            dreaming = data.Bool("dreaming");
        }

        public override void Added(object scene)
        {
            var session = ((OriginalLevelSceneRoot)scene).Session;
            bool oldDreaming = session.Dreaming;
            session.Dreaming = dreaming;
            base.Added(scene);
            session.Dreaming = oldDreaming;
        }

        public override void Update()
        {
            var session = ((OriginalLevelSceneRoot)Scene).Session;
            bool oldDreaming = session.Dreaming;
            session.Dreaming = dreaming;
            base.Update();
            session.Dreaming = oldDreaming;
        }

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new CustomMemorialWithDreamingAttribute(level.EngineContext, data, offset);
    }
}
