using System;
using Celeste.Core.Scheduling;
using Celeste.Game.Gameplay.Holdables.Gliders;
using Celeste.Mods.MaxHelpingHand.Entities.Holdables.Gliders;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Runtime.Holdables.Gliders
{
    public sealed class RespawningJellyfishProcess : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly ILHook hook;

        public RespawningJellyfishProcess(ISourceEngineContext engine)
        {
            this.engine = engine;
            hook = new ILHook(typeof(OriginalGliderState).GetMethod(nameof(OriginalGliderState.Update)), PatchUpdate);
        }

        private void PatchUpdate(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, instruction => instruction.MatchNewobj<SimulationCoroutine>());
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<SimulationCoroutine, OriginalGliderState, SimulationCoroutine>>(SwapCoroutine);
        }

        private SimulationCoroutine SwapCoroutine(SimulationCoroutine original, OriginalGliderState glider) =>
            ReferenceEquals(glider.ActorEngineContext, engine) && glider is RespawningJellyfish jelly
                ? new SimulationCoroutine(engine, jelly.DestroyThenRespawnRoutine())
                : original;

        public void Dispose()
        {
            hook.Dispose();
            RespawningJellyfishParticles.Release(engine);
        }
    }
}
