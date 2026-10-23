using System;
using System.Reflection;
using Celeste.Core.Scheduling;
using Celeste.Mods.MaxHelpingHand.Modules;
using Celeste.Simulation.Gameplay.Player.Movement;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Runtime.Physics.Ice
{
    internal sealed class IcePhysicsHooks : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly MaxHelpingHandModule module;
        private ILHook normalUpdate;

        internal IcePhysicsHooks(ISourceEngineContext engine, MaxHelpingHandModule module)
        {
            this.engine = engine;
            this.module = module;
            normalUpdate =
                new ILHook(
                    typeof(NormalSequence).GetMethod("UpdateCore", BindingFlags.Instance | BindingFlags.NonPublic),
                    OnNormalUpdate);
        }

        private void OnNormalUpdate(ILContext il)
        {
            var cursor = new ILCursor(il);
            int count = 0;
            while (cursor.TryGotoNext(MoveType.After,
                       instruction => instruction.Operand is MethodReference method && method.Name == "get_Cold" &&
                                      method.DeclaringType.FullName == typeof(INormalWorld).FullName))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<bool, NormalSequence, bool>>(ReplaceCold);
                count++;
            }

            if (count != 1)
                throw new InvalidOperationException("Ice physics hook expected 1 normal cold-mode read, found " +
                                                    count + ".");
        }

        private bool ReplaceCold(bool original, NormalSequence sequence) =>
            ReferenceEquals(sequence.EngineContext, engine) && module.Session.IcePhysicsDisabled ? false : original;

        public void Dispose()
        {
            normalUpdate?.Dispose();
            normalUpdate = null;
        }
    }
}
