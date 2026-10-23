using System;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Mechanisms.Switches;
using Celeste.Mods.MaxHelpingHand.Triggers.Switches;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Runtime.Switches
{
    public sealed class TimedTouchSwitchTimerHooks : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly Hook hook;

        public TimedTouchSwitchTimerHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            hook = new Hook(typeof(OriginalTouchSwitchState).GetMethod(nameof(OriginalTouchSwitchState.TurnOn)),
                (Action<Action<OriginalTouchSwitchState>, OriginalTouchSwitchState>)OnTurnOn);
        }

        private void OnTurnOn(Action<OriginalTouchSwitchState> original, OriginalTouchSwitchState self)
        {
            if (self.Scene is OriginalLevelSceneRoot level && ReferenceEquals(level.EngineContext, engine))
            {
                var trigger = level.SourceScene.Entities
                    .GetTrackedEntity<ActivateTimedTouchSwitchesTimerTrigger>();
                trigger?.RestoreTimerOnTouchSwitch(self);
            }

            original(self);
        }

        public void Dispose() => hook.Dispose();
    }
}
