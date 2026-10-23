using System;
using System.Numerics;
using Celeste.Core.Maps;
using Celeste.Core.Scheduling;
using Celeste.Game.Gameplay.Collectibles.Strawberries;
using Celeste.Mods.MaxHelpingHand.Entities.Collectibles.Strawberries.NonPopping;
using Celeste.Simulation;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Runtime.Collectibles.Strawberries.NonPopping
{
    internal sealed class NonPoppingSeedHooks : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly NonPoppingSeedConstruction construction;
        private ILHook strawberryConstructor;
        private ILHook seedUpdate;

        internal NonPoppingSeedHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            construction = NonPoppingSeedConstruction.For(engine);
            try
            {
                strawberryConstructor = new ILHook(typeof(OriginalStrawberryState).GetConstructor(new[]
                {
                    typeof(Celeste.Formats.Map.Entities.Structure.EntityData), typeof(Vector2), typeof(MapEntityId),
                    typeof(IOriginalStrawberryProcess)
                }), ReplaceSeeds);
                seedUpdate =
                    new ILHook(
                        typeof(OriginalStrawberrySeedState).GetMethod(nameof(OriginalStrawberrySeedState.Update)),
                        PreventLosingSeeds);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void ReplaceSeeds(ILContext il)
        {
            var cursor = new ILCursor(il);
            int count = 0;
            while (cursor.TryGotoNext(MoveType.Before,
                       i => i.OpCode == OpCodes.Newobj && i.Operand is MethodReference method &&
                            method.DeclaringType.FullName == typeof(OriginalStrawberrySeedState).FullName))
            {
                cursor.Remove();
                cursor
                    .EmitDelegate<
                        Func<OriginalStrawberryState, Vector2, int, bool, IOriginalStrawberryProcess,
                            OriginalStrawberrySeedState>>(CreateSeed);
                count++;
            }

            if (count != 1)
                throw new InvalidOperationException("Non-popping strawberry expected 1 seed constructor site, found " +
                                                    count + ".");
        }

        private OriginalStrawberrySeedState CreateSeed(OriginalStrawberryState strawberry, Vector2 position,
            int index, bool ghost, IOriginalStrawberryProcess process) =>
            ReferenceEquals(process, engine) && construction.ReplaceSeeds
                ? new NonPoppingStrawberrySeed(strawberry, position, index, ghost, process)
                : new OriginalStrawberrySeedState(strawberry, position, index, ghost, process);

        private void PreventLosingSeeds(ILContext il)
        {
            var cursor = new ILCursor(il);
            int count = 0;
            while (cursor.TryGotoNext(MoveType.After,
                       i => i.Operand is MethodReference method && method.Name == "get_LoseShards" &&
                            method.DeclaringType.FullName == typeof(RoomPlayerSession).FullName))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<bool, OriginalStrawberrySeedState, bool>>(PreventPopping);
                count++;
            }

            if (count != 2)
                throw new InvalidOperationException("Non-popping strawberry expected 2 LoseShards sites, found " +
                                                    count + ".");
        }

        private bool PreventPopping(bool original, OriginalStrawberrySeedState seed) =>
            seed is NonPoppingStrawberrySeed nonPopping && nonPopping.MatchesEngine(engine) ? false : original;

        public void Dispose()
        {
            seedUpdate?.Dispose();
            seedUpdate = null;
            strawberryConstructor?.Dispose();
            strawberryConstructor = null;
        }
    }
}
