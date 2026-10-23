using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Physics.Collision;
using Celeste.Core.Scheduling;
using Celeste.Everest.Reflection.Runtime;
using Celeste.Mods.MaxHelpingHand.Entities;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Collision
{
    internal sealed class ActorOneWayCollisionHooks : IDisposable
    {
        private readonly List<IDisposable> hooks = new();
        private readonly Type movingPlatformType;
        private readonly string ceilingKey;

        private delegate bool MovementResult(bool original, PixelActor actor, int direction, ref PixelPlatform hit,
            DirectionalOneWayCollisionPolicy policy);

        private delegate bool VerticalMovementResult(bool original, PixelActor actor, int direction,
            ref PixelPlatform hit,
            List<object> ceilings);

        public ActorOneWayCollisionHooks(ISourceEngineContext engine, bool horizontal, bool vertical)
        {
            if (vertical)
            {
                movingPlatformType = ((ISourceRuntimeTypeDiscoveryEnvironment)engine).RuntimeTypeDiscovery
                    .ResolveRuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.UpsideDownMovingPlatform");
                ceilingKey = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog.Find(typeof(UpsideDownJumpThru))
                    .RegistryKey;
            }

            try
            {
                if (horizontal)
                    hooks.Add(new ILHook(typeof(PixelActor).GetMethod(nameof(PixelActor.MoveHExact)), PatchHorizontal));
                if (vertical)
                    hooks.Add(new ILHook(typeof(PixelActor).GetMethod(nameof(PixelActor.MoveVExact)), PatchVertical));
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private static VariableDefinition SignLocal(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, i => i.MatchCall(typeof(Math), nameof(Math.Sign)));
            if (!cursor.Next.MatchStloc(out int index))
                throw new InvalidOperationException("The native Actor movement sign local is missing.");
            return il.Body.Variables[index];
        }

        private static VariableDefinition HitLocal(ILCursor cursor)
            => cursor.Prev.Previous.Operand as VariableDefinition
               ?? throw new InvalidOperationException("The native Actor collision out local is missing.");

        private static void PatchHorizontal(ILContext il)
        {
            var sign = SignLocal(il);
            var policy = new VariableDefinition(il.Import(typeof(DirectionalOneWayCollisionPolicy)));
            il.Body.Variables.Add(policy);
            var cursor = new ILCursor(il);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<PixelActor, DirectionalOneWayCollisionPolicy>>(actor =>
                actor.SourceColliderOwner == null ? null : DirectionalCollisionRegistration.Get(actor.World));
            cursor.Emit(OpCodes.Stloc, policy);
            cursor.GotoNext(MoveType.After, i => i.MatchCall(typeof(PixelActor), "SolidAt"));
            var hit = HitLocal(cursor);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldloc, sign);
            cursor.Emit(OpCodes.Ldloca, hit);
            cursor.Emit(OpCodes.Ldloc, policy);
            cursor.EmitDelegate<MovementResult>(Horizontal);
        }

        private static bool Horizontal(bool original, PixelActor actor, int direction, ref PixelPlatform hit,
            DirectionalOneWayCollisionPolicy policy)
        {
            if (original || policy == null) return original;
            var collider = policy.Movement(actor.SourceColliderOwner,
                actor.Position + Vector2.UnitX * direction, direction);
            if (collider == null) return false;
            hit = ((SourcePlatformEntityState)collider.GetMovementCollisionTarget(actor.SourceColliderOwner)).Platform;
            return true;
        }

        private void PatchVertical(ILContext il)
        {
            var sign = SignLocal(il);
            var policy = new VariableDefinition(il.Import(typeof(List<object>)));
            il.Body.Variables.Add(policy);
            var cursor = new ILCursor(il);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<PixelActor, List<object>>>(Ceilings);
            cursor.Emit(OpCodes.Stloc, policy);
            cursor.GotoNext(MoveType.After, i => i.MatchCall(typeof(PixelActor), "SolidAt"));
            var hit = HitLocal(cursor);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldloc, sign);
            cursor.Emit(OpCodes.Ldloca, hit);
            cursor.Emit(OpCodes.Ldloc, policy);
            cursor.EmitDelegate<VerticalMovementResult>(Vertical);
            cursor.GotoNext(MoveType.After, i => i.MatchCall(typeof(PixelActor), "JumpThruOutside"));
            hit = HitLocal(cursor);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldloc, sign);
            cursor.Emit(OpCodes.Ldloca, hit);
            cursor.Emit(OpCodes.Ldloc, policy);
            cursor.EmitDelegate<VerticalMovementResult>(Floor);
        }

        private List<object> Ceilings(PixelActor actor)
            => (actor.SourceColliderOwner as SceneEntityState)?.Scene is ISourceScene scene
                ? scene.SourceScene.Entities.GetTrackedEntities(ceilingKey)
                : null;

        private static bool Vertical(bool original, PixelActor actor, int direction, ref PixelPlatform hit,
            List<object> ceilings)
        {
            if (original || direction >= 0 || actor.IgnoreJumpThrus || ceilings == null) return original;
            Vector2 at = actor.Position - Vector2.UnitY;
            for (int i = 0; i < ceilings.Count; i++)
            {
                var ceiling = (UpsideDownJumpThru)ceilings[i];
                if (SourceEntityCollision.Check(actor.SourceColliderOwner, ceiling) ||
                    !SourceEntityCollision.CheckAt(actor.SourceColliderOwner, ceiling, at)) continue;
                hit = ceiling.Platform;
                return true;
            }

            return false;
        }

        private bool Floor(bool original, PixelActor actor, int direction, ref PixelPlatform hit,
            List<object> ceilings)
        {
            if (!original || hit?.SourceEntityOwner == null) return original;
            Type type = hit.SourceEntityOwner.GetType();
            return type != typeof(UpsideDownJumpThru) && type != movingPlatformType;
        }

        public void Dispose()
        {
            for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
            hooks.Clear();
        }
    }
}
