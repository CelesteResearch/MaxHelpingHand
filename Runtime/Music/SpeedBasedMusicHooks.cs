using System;
using System.Runtime.CompilerServices;
using Celeste.Core.Mathematics;
using Celeste.Core.Scheduling;
using Celeste.Game.Audio;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Mods.MaxHelpingHand.Sessions;
using Celeste.Simulation;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Runtime.Music
{
    public sealed class SpeedBasedMusicHooks : IDisposable
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, SpeedBasedMusicHooks> owners = new();
        private readonly ISourceEngineContext engine;
        private readonly Func<MaxHelpingHandSession> session;
        private readonly Hook update;
        public MaxHelpingHandSession Session => session();

        public static SpeedBasedMusicHooks For(ISourceEngineContext engine) => owners.TryGetValue(engine, out var value)
            ? value
            : throw new InvalidOperationException("MaxHelpingHand music hooks are not loaded for this engine.");

        public SpeedBasedMusicHooks(ISourceEngineContext engine, Func<MaxHelpingHandSession> session)
        {
            this.engine = engine;
            this.session = session;
            owners.Add(engine, this);
            try
            {
                update = new Hook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.Update)),
                    (Action<Action<RoomPlayerSession>, RoomPlayerSession>)OnPlayerUpdate);
            }
            catch
            {
                owners.Remove(engine);
                throw;
            }
        }

        private void OnPlayerUpdate(Action<RoomPlayerSession> original, RoomPlayerSession player)
        {
            original(player);
            if (!ReferenceEquals(player.EngineContext, engine) ||
                Session.ActiveSpeedBasedMusicParams.Count == 0) return;
            var audio = ((OriginalLevelSceneRoot)player.Scene).Session.Audio;
            float speed = ((ISourceVectorEnvironment)engine).VectorArithmetic.Length(player.Data.Motion.Normal.Speed);
            foreach (var parameter in Session.ActiveSpeedBasedMusicParams)
                audio.Music.Param(parameter.Key, ScalarMath.ClampedMap(speed,
                    parameter.Value.MinimumSpeed, parameter.Value.MaximumSpeed,
                    parameter.Value.MinimumParamValue, parameter.Value.MaximumParamValue));
            var process = (IOriginalGameplayProcess)engine;
            if (audio.Music.Event == process.AudioSession.CurrentMusic)
                OriginalAudioSession.Apply(audio, process.AudioSession.ChannelOutput, process.AudioApplyPolicy);
        }

        public void Dispose()
        {
            update.Dispose();
            owners.Remove(engine);
        }
    }
}
