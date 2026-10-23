using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Everest.Runtime.Loading;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.MaxHelpingHand.Triggers.Flags;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Presentation.Sprites;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Runtime.Flags.Spawn
{
    internal sealed class SpawnFlagHooks : IDisposable
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, SpawnFlagHooks> owners = new();
        private readonly ISourceEngineContext engine;
        private readonly Hook hook;
        internal readonly List<SetFlagOnSpawnTrigger> Triggers = new();

        internal static SpawnFlagHooks For(ISourceEngineContext engine) =>
            owners.GetValue(engine, e => new SpawnFlagHooks(e));

        private SpawnFlagHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            hook = new Hook(
                typeof(EverestLevelLoadOverrides).GetMethod("LoadNewPlayer",
                    BindingFlags.NonPublic | BindingFlags.Static),
                (Func<Func<Vector2, PlayerSpriteMode, OriginalLevelSceneRoot, RoomPlayerSession>, Vector2,
                    PlayerSpriteMode, OriginalLevelSceneRoot, RoomPlayerSession>)LoadPlayer);
        }

        private RoomPlayerSession LoadPlayer(
            Func<Vector2, PlayerSpriteMode, OriginalLevelSceneRoot, RoomPlayerSession> original,
            Vector2 position, PlayerSpriteMode mode, OriginalLevelSceneRoot level)
        {
            if (ReferenceEquals(level.EngineContext, engine))
                foreach (var trigger in Triggers)
                    trigger.SetFlags(level, position);
            return original(position, mode, level);
        }

        public void Dispose()
        {
            hook.Dispose();
            owners.Remove(engine);
        }
    }
}
