using System.Numerics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Audio;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Mods.MaxHelpingHand.Runtime.Music;
using Celeste.Mods.MaxHelpingHand.Sessions.Music;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.MaxHelpingHand.Triggers.Audio
{
    [CustomEntity("MaxHelpingHand/SpeedBasedMusicParamTrigger = Load")]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Triggers.SpeedBasedMusicParamTrigger")]
    public class SpeedBasedMusicParamTrigger : SourceTriggerEntityState
    {
        private readonly ISourceEngineContext engine;
        private string paramName;
        private float minSpeed, maxSpeed, minParamValue, maxParamValue;
        private bool activate;

        public static SpeedBasedMusicParamTrigger Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(level.EngineContext, data, offset);

        public SpeedBasedMusicParamTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(
            ((Celeste.Core.Components.ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
            data.Position + offset, data.Width, data.Height,
            ((Celeste.Core.Mathematics.Extrema.ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            this.engine = engine;
            paramName = data.Attr("paramName");
            minSpeed = data.Float("minSpeed");
            maxSpeed = data.Float("maxSpeed");
            minParamValue = data.Float("minParamValue");
            maxParamValue = data.Float("maxParamValue");
            activate = data.Bool("activate");
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            base.OnEnter(player);
            var parameters = SpeedBasedMusicHooks.For(engine).Session.ActiveSpeedBasedMusicParams;
            if (activate)
                parameters[paramName] = new SpeedBasedMusicParamInfo
                {
                    MinimumSpeed = minSpeed, MaximumSpeed = maxSpeed,
                    MinimumParamValue = minParamValue, MaximumParamValue = maxParamValue
                };
            else
            {
                parameters.Remove(paramName);
                var audio = ((OriginalLevelSceneRoot)Scene).Session.Audio;
                audio.Music.Param(paramName, minParamValue);
                var process = (IOriginalGameplayProcess)engine;
                OriginalAudioSession.Apply(audio, process.AudioSession.ChannelOutput, process.AudioApplyPolicy);
            }
        }
    }
}
