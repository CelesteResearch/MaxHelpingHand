using System.Numerics;
using Celeste.Core.Audio;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Gameplay.Enemies.Badeline.Chasers;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Tracking.Attributes;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Gameplay.Enemies.Badeline.Chasers;

namespace Celeste.Mods.MaxHelpingHand.Entities.Enemies.Badeline.Chasers
{
    [CustomEntity("MaxHelpingHand/FlagBadelineChaser")]
    [EverestTrackedAs(typeof(BadelineOldsiteState))]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.FlagBadelineChaser")]
    public class FlagBadelineChaser : EverestBadelineOldsiteState
    {
        private readonly string flagName;
        private bool started;

        public static FlagBadelineChaser Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(data, offset, level.EngineContext);

        public FlagBadelineChaser(EntityData data, Vector2 offset, ISourceEngineContext engine)
            : base(data, offset, EntityAttributeReader.Int(data.Values, "index"), engine)
        {
            flagName = EntityAttributeReader.Attr(data.Values, "flag");
        }

        public override void Update()
        {
            if (started || ((OriginalLevelSceneRoot)Scene).Session.GetFlag(flagName))
            {
                base.Update();
                started = true;
            }

            if (started && !((OriginalLevelSceneRoot)Scene).Session.GetFlag(flagName))
            {
                var level = (OriginalLevelSceneRoot)Scene;
                ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play("event:/char/badeline/disappear", Position);
                level.Displacement.AddBurst(Center, .5f, 24f, 96f, .4f);
                level.Particles.Emit(((IOriginalGameplayProcess)engine).ParticleTypes.BadelineOldsite.P_Vanish, 12,
                    Center, Vector2.One * 6f);
                RemoveSelf();
            }
        }
    }
}
