// Copyright (c) 2019 maddie480. Licensed under the MIT License.

using System;
using System.Numerics;
using System.Reflection;
using Celeste.Core.Scheduling;
using Celeste.Core.Mathematics;
using Celeste.Game.Gameplay.Hazards.Puffer;
using Celeste.Mods.MaxHelpingHand.Entities.Hazards;
using Celeste.Scene.Components.Oscillation;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Movement;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Gameplay.Hazards
{
    public sealed class StaticPufferHooks : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private ILHook constructorHook;
        private IDisposable explodeHook;
        private ILHook launchHook;
        private int? currentDownboostTolerance;
        private bool currentPufferFacesRight;

        public StaticPufferHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            try
            {
                constructorHook = new ILHook(typeof(OriginalPufferState).GetConstructor(new[]
                {
                    typeof(Celeste.Physics.Collision.PixelActor), typeof(Vector2), typeof(bool),
                    typeof(ISourceEngineContext),
                    typeof(Celeste.Simulation.Gameplay.Platforms.Foundation.IPlatformTargetMotionPolicy)
                }), ModifyConstructor);
                explodeHook = new Hook(
                    typeof(OriginalPufferState).GetMethod("Explode", BindingFlags.Instance | BindingFlags.NonPublic),
                    (Action<Action<OriginalPufferState>, OriginalPufferState>)Explode);
                launchHook = new ILHook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.ExplodeLaunch)),
                    ModifyLaunch);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void ModifyConstructor(ILContext il)
        {
            var cursor = new ILCursor(il);
            bool found = false;
            while (cursor.TryGotoNext(MoveType.After,
                       i => i.MatchCallOrCallvirt<SineWaveState>(nameof(SineWaveState.Randomize))))
            {
                found = true;
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg, 4);
                cursor.EmitDelegate<Action<OriginalPufferState, ISourceEngineContext>>(ResetSine);
            }

            if (!found) throw new InvalidOperationException("Missing Puffer SineWave.Randomize constructor entry.");
        }

        private void ResetSine(OriginalPufferState self, ISourceEngineContext owner)
        {
            if (ReferenceEquals(owner, engine) && self is StaticPuffer puffer) puffer.ResetIdleSine();
        }

        private void Explode(Action<OriginalPufferState> orig, OriginalPufferState self)
        {
            if (!ReferenceEquals(((ISourceEngineContextScene)self.Scene).EngineContext, engine))
            {
                orig(self);
                return;
            }

            if (self is StaticPuffer puffer)
            {
                currentDownboostTolerance = puffer.DownboostTolerance;
                currentPufferFacesRight = puffer.FacesRight;
            }

            orig(self);
            currentDownboostTolerance = null;
        }

        private void ModifyLaunch(ILContext il)
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(i => i.MatchStfld<NormalMotionState>(nameof(NormalMotionState.Speed))))
                throw new InvalidOperationException("Missing Player.ExplodeLaunch initial Speed store.");
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Func<Vector2, RoomPlayerSession, Vector2, Vector2>>(ModifyVelocity);
        }

        private Vector2 ModifyVelocity(Vector2 original, RoomPlayerSession player, Vector2 from)
        {
            if (currentDownboostTolerance == null || !ReferenceEquals(player.EngineContext, engine)) return original;
            var arithmetic = ((ISourceVectorEnvironment)engine).VectorArithmetic;
            float offsetX = Math.Abs(player.Center.X - from.X);
            if (offsetX <= currentDownboostTolerance)
            {
                if (original.Y != 0) return original;
                return arithmetic.SafeNormalize(player.Center - from, -Vector2.UnitY) * arithmetic.Length(original);
            }

            if (original.Y == 0) return original;
            return Vector2.UnitX * arithmetic.Length(original) * (currentPufferFacesRight ? 1 : -1);
        }

        public void Dispose()
        {
            launchHook?.Dispose();
            launchHook = null;
            explodeHook?.Dispose();
            explodeHook = null;
            constructorHook?.Dispose();
            constructorHook = null;
            currentDownboostTolerance = null;
        }
    }
}
