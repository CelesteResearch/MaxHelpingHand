using System;
using Celeste.Core.Scheduling;
using Celeste.Game.Entities.PlaybackBillboard;
using Celeste.Mods.MaxHelpingHand.Entities.Scenery.Billboards;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Runtime.Scenery.Billboards
{
    internal sealed class ReskinnableBillboardHooks : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private ILHook awake;

        internal ReskinnableBillboardHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            awake = new ILHook(typeof(PlaybackBillboardState).GetMethod(nameof(PlaybackBillboardState.Awake)), OnAwake);
        }

        private void OnAwake(ILContext il)
        {
            var cursor = new ILCursor(il);
            int count = 0;
            while (cursor.TryGotoNext(MoveType.After,
                       instruction => instruction.OpCode == OpCodes.Ldstr &&
                                      Equals(instruction.Operand, "scenery/tvSlices")))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<string, PlaybackBillboardState, string>>(ReplaceTexture);
                count++;
            }

            if (count != 1)
                throw new InvalidOperationException("Billboard hook expected 1 border texture request, found " + count +
                                                    ".");
        }

        private string ReplaceTexture(string original, PlaybackBillboardState billboard) =>
            billboard is ReskinnableBillboard reskinned && reskinned.MatchesEngine(engine)
                ? reskinned.BorderTexture
                : original;

        public void Dispose()
        {
            awake?.Dispose();
            awake = null;
        }
    }
}
