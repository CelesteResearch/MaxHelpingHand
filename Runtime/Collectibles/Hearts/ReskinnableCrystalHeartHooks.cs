using System;
using System.Reflection;
using Celeste.Core.Scheduling;
using Celeste.Game.Entities.Collectibles.Hearts;
using Celeste.Mods.MaxHelpingHand.Entities.Collectibles.Hearts;
using Celeste.Simulation;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Runtime.Collectibles.Hearts
{
    internal sealed class ReskinnableCrystalHeartHooks : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private ILHook awake;
        private Hook collect;
        private ILHook update;

        internal ReskinnableCrystalHeartHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            try
            {
                awake = new ILHook(typeof(OriginalHeartGemState).GetMethod(nameof(OriginalHeartGemState.Awake)),
                    OnAwake);
                collect = new Hook(
                    typeof(OriginalHeartGemState).GetMethod("Collect", BindingFlags.Instance | BindingFlags.NonPublic),
                    (Action<Action<OriginalHeartGemState, RoomPlayerSession>, OriginalHeartGemState, RoomPlayerSession>)
                    OnCollect);
                update = new ILHook(typeof(OriginalHeartGemState).GetMethod(nameof(OriginalHeartGemState.Update)),
                    OnUpdate);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void OnAwake(ILContext il)
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.AfterLabel,
                    instruction =>
                        instruction.MatchStfld<OriginalHeartGemState>(nameof(OriginalHeartGemState.IsGhost))))
                throw new InvalidOperationException("Heart Awake ghost store missing.");
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<bool, OriginalHeartGemState, bool>>(SelectGhost);
            if (!cursor.TryGotoNext(MoveType.Before,
                    instruction => instruction.Operand is Mono.Cecil.MethodReference method &&
                                   method.Name == "Create" && method.Parameters.Count == 1 &&
                                   method.Parameters[0].ParameterType.FullName == "System.String"))
                throw new InvalidOperationException("Heart Awake sprite creation missing.");
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<string, OriginalHeartGemState, string>>(SelectSprite);
        }

        private bool SelectGhost(bool original, OriginalHeartGemState self) =>
            self is ReskinnableCrystalHeart heart && heart.MatchesEngine(engine)
                ? heart.SelectGhost(original)
                : original;

        private string SelectSprite(string original, OriginalHeartGemState self) =>
            self is ReskinnableCrystalHeart heart && heart.MatchesEngine(engine)
                ? heart.SelectSprite(original)
                : original;

        private void OnCollect(Action<OriginalHeartGemState, RoomPlayerSession> original, OriginalHeartGemState self,
            RoomPlayerSession player)
        {
            if (self is ReskinnableCrystalHeart heart && heart.MatchesEngine(engine)) heart.BeforeCollect();
            original(self, player);
        }

        private void OnUpdate(ILContext il)
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.Before, instruction => instruction.MatchLdarg(0),
                    instruction => instruction.MatchLdfld<OriginalHeartGemState>("sprite"),
                    instruction => instruction.MatchCall<System.Numerics.Vector2>("get_UnitY")))
                throw new InvalidOperationException("Heart Update wiggle start missing.");
            int start = cursor.Index;
            if (!cursor.TryGotoNext(MoveType.After,
                    instruction => instruction.OpCode == OpCodes.Stfld &&
                                   instruction.Operand is Mono.Cecil.FieldReference field && field.Name == "Position"))
                throw new InvalidOperationException("Heart Update wiggle end missing.");
            var skip = cursor.MarkLabel();
            cursor.Index = start;
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<OriginalHeartGemState, bool>>(TryRotatedWiggle);
            cursor.Emit(OpCodes.Brtrue, skip);
        }

        private bool TryRotatedWiggle(OriginalHeartGemState self)
        {
            if (!(self is ReskinnableCrystalHeart heart) || !heart.MatchesEngine(engine)) return false;
            heart.RotatedWiggle();
            return true;
        }

        public void Dispose()
        {
            update?.Dispose();
            update = null;
            collect?.Dispose();
            collect = null;
            awake?.Dispose();
            awake = null;
        }
    }
}
