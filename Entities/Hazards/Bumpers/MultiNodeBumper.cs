using System.Numerics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.MaxHelpingHand.Entities.Platforms;
using Celeste.Mods.MaxHelpingHand.Runtime.Hazards.Bumpers;
using Celeste.Simulation.Gameplay.Platforms.Collision;

namespace Celeste.Mods.MaxHelpingHand.Entities.Hazards.Bumpers
{
    [CustomEntity("MaxHelpingHand/MultiNodeBumper")]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.MultiNodeBumper")]
    public class MultiNodeBumper : BumperNotCoreMode
    {
        private readonly EntityData thisEntityData;
        private readonly Vector2 thisOffset;
        private MultiNodeMovingPlatform animatingPlatform;
        private bool spawnedByOtherBumper;

        public MultiNodeBumper(EntityData data, Vector2 offset, Celeste.Core.Scheduling.ISourceEngineContext engine)
            : base(data.Position + offset, null, data.Bool("notCoreMode", false), data.Bool("wobble", false), engine)
        {
            thisEntityData = data;
            thisOffset = offset;
        }

        public static MultiNodeBumper Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new MultiNodeBumper(data, offset, level.EngineContext);

        public override void Added(object scene)
        {
            base.Added(scene);
            if (spawnedByOtherBumper) return;
            var mover = MakeWobbleStaticMover();
            Add(mover);
            var level = (OriginalLevelSceneRoot)scene;
            animatingPlatform = new MultiNodeMovingPlatform(thisEntityData, thisOffset,
                ((ISourcePlatformCollisionScene)level).SolidWorldFactory, otherPlatform =>
                {
                    if (otherPlatform != animatingPlatform)
                    {
                        var otherBumper = new MultiNodeBumper(thisEntityData, thisOffset, engine)
                            { spawnedByOtherBumper = true, PresentationOwner = this };
                        ((OriginalLevelSceneRoot)Scene).Scheduler.QueueSourceEntity(otherBumper);
                        var otherMover = otherBumper.MakeWobbleStaticMover();
                        otherBumper.Add(otherMover);
                        MovingPlatformAttachment.Animate(otherPlatform, otherMover, null);
                    }
                });
            MovingPlatformAttachment.Animate(animatingPlatform, mover, null);
            level.Scheduler.QueueSourceEntity(animatingPlatform);
        }
    }
}
