using System;
using System.Reflection;
using Celeste.Core.Scheduling;
using Celeste.Game.Gameplay.Hazards.Bumpers;
using Celeste.Mods.MaxHelpingHand.Entities.Hazards.Bumpers;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Runtime.Hazards.Bumpers
{
    internal sealed class BumperNotCoreModeHooks : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private Hook updatePosition;

        internal BumperNotCoreModeHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            updatePosition = new Hook(
                typeof(OriginalBumperState).GetMethod("UpdatePosition", BindingFlags.Instance | BindingFlags.NonPublic),
                (Action<Action<OriginalBumperState>, OriginalBumperState>)OnUpdatePosition);
        }

        private void OnUpdatePosition(Action<OriginalBumperState> original, OriginalBumperState self)
        {
            if (!(self is BumperNotCoreMode bumper) || !bumper.MatchesEngine(engine)) original(self);
        }

        public void Dispose()
        {
            updatePosition?.Dispose();
            updatePosition = null;
        }
    }
}
