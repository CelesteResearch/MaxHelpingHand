using System.Numerics;
using Celeste.Core.Mathematics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Audio;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Gameplay.Triggers.Audio;
using Celeste.Simulation;

namespace Celeste.Mods.MaxHelpingHand.Triggers.Audio
{
    [CustomEntity("MaxHelpingHand/PersistentMusicFadeTrigger = Load")]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Triggers.PersistentMusicFadeTrigger")]
    public class PersistentMusicFadeTrigger : InstalledMusicFadeTriggerState
    {
        private readonly ISourceEngineContext engine;

        public static PersistentMusicFadeTrigger Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(level.EngineContext, data, offset);

        public PersistentMusicFadeTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(data, offset, engine)
        {
            this.engine = engine;
            if (string.IsNullOrEmpty(Parameter)) Parameter = "fade";
        }

        public override void OnStay(RoomPlayerSession player)
        {
            float value = LeftToRight
                ? ScalarMath.ClampedMap(player.Center.X, Left, Right, FadeA, FadeB)
                : ScalarMath.ClampedMap(player.Center.Y, Top, Bottom, FadeA, FadeB);
            var audio = ((OriginalLevelSceneRoot)Scene).Session.Audio;
            audio.Music.Param(Parameter, value);
            var process = (IOriginalGameplayProcess)engine;
            OriginalAudioSession.Apply(audio, process.AudioSession.ChannelOutput, process.AudioApplyPolicy);
        }
    }
}
