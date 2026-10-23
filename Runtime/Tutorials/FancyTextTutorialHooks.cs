using System;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Everest.Entities.Birds.Tutorials;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.MaxHelpingHand.Entities.Tutorials;
using Celeste.Mods.MaxHelpingHand.Runtime.Backdrops;
using Celeste.Simulation;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Runtime.Tutorials
{
    public sealed class FancyTextTutorialHooks : IDisposable
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, FancyTextTutorialHooks> owners = new();
        private readonly ISourceEngineContext engine;
        private readonly MaxBackdropBufferState buffers;
        private readonly Hook enterHook;
        public bool ZoomOutEnabled => buffers.ZoomOutEnabled;

        private FancyTextTutorialHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            buffers = new MaxBackdropBufferState(engine);
            enterHook = new Hook(typeof(CustomBirdTutorialTrigger).GetMethod(nameof(CustomBirdTutorialTrigger.OnEnter)),
                (Action<Action<CustomBirdTutorialTrigger, RoomPlayerSession>, CustomBirdTutorialTrigger,
                    RoomPlayerSession>)OnEnter);
        }

        public static FancyTextTutorialHooks For(ISourceEngineContext engine) =>
            owners.GetValue(engine, e => new FancyTextTutorialHooks(e));

        private void OnEnter(Action<CustomBirdTutorialTrigger, RoomPlayerSession> original,
            CustomBirdTutorialTrigger self, RoomPlayerSession player)
        {
            original(self, player);
            if (!ReferenceEquals(player.EngineContext, engine)) return;
            var bird = FancyTextTutorial.FindById(self.Scene as OriginalLevelSceneRoot, self.BirdId);
            if (bird != null)
            {
                if (self.ShowTutorial) bird.TriggerShowTutorial();
                else bird.TriggerHideTutorial();
            }
        }

        public void Dispose()
        {
            enterHook.Dispose();
            buffers.Dispose();
            owners.Remove(engine);
        }
    }
}
