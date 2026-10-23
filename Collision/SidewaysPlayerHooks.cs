using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using Celeste.Core.Scheduling;
using Celeste.Physics.Collision;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Collision.Contacts;
using Celeste.Simulation.Gameplay.Player.Movement;
using Celeste.Simulation.Gameplay.Player.Runtime;
using Celeste.Simulation.Gameplay.Player.Audio.Frame;
using Celeste.Simulation.Gameplay.Player.Presentation.Animation;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Collision
{
    internal sealed class SidewaysPlayerHooks : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly List<IDisposable> hooks = new();

        // The owner activates these with the same chapter-map lifecycle as the native mod.
        internal SidewaysPlayerHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            try
            {
                hooks.Add(new ILHook(typeof(PlayerContacts).GetMethod(nameof(PlayerContacts.ClimbCheck)),
                    il => PatchContacts(il, DirectionalCollisionFeature.Climb)));
                hooks.Add(new ILHook(typeof(PlayerContacts).GetMethod(nameof(PlayerContacts.WallJumpCheck)),
                    il => PatchContacts(il, DirectionalCollisionFeature.WallJump)));
                hooks.Add(new ILHook(typeof(PlayerContacts).GetMethod(nameof(PlayerContacts.SlipCheck)), PatchSlip));
                hooks.Add(new ILHook(typeof(PlayerFrameMotion).GetMethod(nameof(PlayerFrameMotion.Update)),
                    PatchFrame));
                PatchWorld(typeof(INormalWorld), nameof(INormalWorld.SolidAt), DirectionalCollisionFeature.WallJump);
                PatchWorld(typeof(IClimbWorld), nameof(IClimbWorld.SolidAt), DirectionalCollisionFeature.Climb);
                PatchWorld(typeof(IPlayerMovementWorld), nameof(IPlayerMovementWorld.SolidAt),
                    DirectionalCollisionFeature.Neutral);
                PatchWorld(typeof(IPlayerAnimationWorld), nameof(IPlayerAnimationWorld.SolidAt),
                    DirectionalCollisionFeature.Neutral);
                PatchWorld(
                    typeof(Celeste.Simulation.Gameplay.Player.Collision.Responses.IPlayerDirectionalCollisionWorld),
                    "HorizontalSolidAt", DirectionalCollisionFeature.Neutral);
                var pointMap = typeof(RoomPlayerWorld).GetInterfaceMap(typeof(IPlayerAnimationWorld));
                for (int i = 0; i < pointMap.InterfaceMethods.Length; i++)
                    if (pointMap.InterfaceMethods[i].Name == nameof(IPlayerAnimationWorld.SolidPointAt))
                        hooks.Add(new ILHook(pointMap.TargetMethods[i], PatchWorldPoint));
                hooks.Add(new Hook(typeof(PlayerContacts).GetMethod(nameof(PlayerContacts.DuckFreeAt)),
                    (Func<Func<PlayerContacts, Vector2, bool>, PlayerContacts, Vector2, bool>)DuckFree));
                hooks.Add(new Hook(typeof(PlayerContacts).GetMethod(nameof(PlayerContacts.ClimbHopBlocked)),
                    (Func<Func<PlayerContacts, bool, bool>, PlayerContacts, bool, bool>)ClimbHopBlocked));
                hooks.Add(new Hook(typeof(NormalSequence).GetMethod(nameof(NormalSequence.Update)),
                    (Func<Func<NormalSequence, int>, NormalSequence, int>)NormalUpdate));
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void PatchWorld(Type contract, string name, DirectionalCollisionFeature feature)
        {
            var map = typeof(RoomPlayerWorld).GetInterfaceMap(contract);
            for (int i = 0; i < map.InterfaceMethods.Length; i++)
                if (map.InterfaceMethods[i].Name == name)
                {
                    hooks.Add(new ILHook(map.TargetMethods[i], il => PatchContacts(il, feature)));
                    return;
                }

            throw new MissingMethodException(contract.FullName, name);
        }

        private void PatchContacts(ILContext il, DirectionalCollisionFeature feature)
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.Before,
                    i => i.MatchCall<PlayerContacts>(nameof(PlayerContacts.SolidAt)) ||
                         i.MatchCallvirt<PlayerContacts>(nameof(PlayerContacts.SolidAt))))
                throw new InvalidOperationException("Missing native SolidAt query in " + il.Method.FullName);
            var offset = new VariableDefinition(il.Import(typeof(Vector2)));
            var contacts = new VariableDefinition(il.Import(typeof(PlayerContacts)));
            il.Body.Variables.Add(offset);
            il.Body.Variables.Add(contacts);
            cursor.Emit(OpCodes.Stloc, offset);
            cursor.Emit(OpCodes.Dup);
            cursor.Emit(OpCodes.Stloc, contacts);
            cursor.Emit(OpCodes.Ldloc, offset);
            cursor.Index++;
            // Preserve the native call for other mods, then consume the completed boolean chain.
            // Existing short-circuit branches still target the native ret and skip this query-only addition.
            while (cursor.Next != null && cursor.Next.OpCode != OpCodes.Ret)
            {
                if (cursor.Next.OpCode.FlowControl is FlowControl.Branch or FlowControl.Cond_Branch)
                    throw new InvalidOperationException("Unexpected control flow after final SolidAt query");
                cursor.Index++;
            }

            if (cursor.Next == null) throw new InvalidOperationException("Missing native boolean return");
            cursor.Emit(OpCodes.Ldloc, contacts);
            cursor.Emit(OpCodes.Ldloc, offset);
            cursor.EmitDelegate<Func<bool, PlayerContacts, Vector2, bool>>((original, owner, at) =>
                EntityContact(original, owner, at, feature));
        }

        private void PatchSlip(ILContext il)
        {
            var cursor = new ILCursor(il);
            var point = new Mono.Cecil.Cil.VariableDefinition(il.Import(typeof(Vector2)));
            il.Body.Variables.Add(point);
            int count = 0;
            while (cursor.TryGotoNext(MoveType.Before,
                       i => i.MatchCallvirt<ICollisionWorld>(nameof(ICollisionWorld.SolidAt)) &&
                            ((Mono.Cecil.MethodReference)i.Operand).Parameters[0].ParameterType.FullName ==
                            typeof(Vector2).FullName))
            {
                cursor.Emit(OpCodes.Stloc, point);
                cursor.Emit(OpCodes.Ldloc, point);
                cursor.Index++;
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloc, point);
                cursor.EmitDelegate<Func<bool, PlayerContacts, Vector2, bool>>((original, contacts, at) =>
                    original || Own(contacts) &&
                    DirectionalCollisionRegistration.Get(contacts.Body.World)?.PointContact(at) != null);
                count++;
            }

            if (count != 2) throw new InvalidOperationException("Expected both native SlipCheck point queries");
        }

        private void PatchFrame(ILContext il)
        {
            var cursor = new ILCursor(il);
            int count = 0;
            while (cursor.TryGotoNext(MoveType.Before,
                       i => i.MatchCallvirt<IPlayerFrameWorld>(nameof(IPlayerFrameWorld.SolidAt))))
            {
                cursor.Remove();
                cursor.EmitDelegate<Func<IPlayerFrameWorld, Vector2, bool>>((world, offset) =>
                    world.SolidAt(offset) || world is RoomPlayerWorld room &&
                    EntityContact(false, room.Contacts, offset, DirectionalCollisionFeature.Neutral));
                count++;
            }

            if (count != 2) throw new InvalidOperationException("Expected native retention and climb-hop wait queries");
        }

        private void PatchWorldPoint(ILContext il)
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.Before,
                    i => i.MatchCall<RoomPlayerWorld>(nameof(RoomPlayerWorld.SolidPointAt))))
                throw new InvalidOperationException("Missing native animation point query");
            cursor.Remove();
            cursor.EmitDelegate<Func<RoomPlayerWorld, Vector2, bool>>((world, offset) =>
                world.SolidPointAt(offset) || ReferenceEquals(world.EngineContext, engine) &&
                DirectionalCollisionRegistration.Get(world.Platforms)?.PointContact(world.Position + offset) != null);
        }

        private bool Own(PlayerContacts contacts)
            => ReferenceEquals(((RoomPlayerSession)contacts.ActualOwner).EngineContext, engine);

        private bool EntityContact(bool original, PlayerContacts contacts, Vector2 offset,
            DirectionalCollisionFeature feature)
            => original || offset.X != 0f && Own(contacts) &&
                DirectionalCollisionRegistration.Get(contacts.Body.World)?.EntityContact(contacts.ActualOwner,
                    contacts.Body.Position + offset, feature) != null;

        private bool DuckFree(Func<PlayerContacts, Vector2, bool> original, PlayerContacts contacts, Vector2 offset)
        {
            if (!original(contacts, offset)) return false;
            if (!Own(contacts)) return true;
            var policy = DirectionalCollisionRegistration.Get(contacts.Body.World);
            if (policy == null) return true;
            var saved = contacts.Collider;
            contacts.SetCollider(contacts.DuckHitbox);
            bool result = policy.EntityContact(contacts.ActualOwner, contacts.Body.Position + offset) == null;
            contacts.SetCollider(saved);
            return result;
        }

        private bool ClimbHopBlocked(Func<PlayerContacts, bool, bool> original, PlayerContacts contacts, bool seed)
            => original(contacts, seed) || Own(contacts) &&
                DirectionalCollisionRegistration.Get(contacts.Body.World)?.FirstOutside(contacts.ActualOwner,
                    contacts.Body.Position + Vector2.UnitX * contacts.State.Facing) != null;

        private int NormalUpdate(Func<NormalSequence, int> original, NormalSequence sequence)
        {
            int result = original(sequence);
            if (!ReferenceEquals(sequence.EngineContext, engine) || sequence.World is not RoomPlayerWorld world ||
                sequence.Data.Motion.Normal.Speed.X == 0f) return result;
            int direction = Math.Sign(sequence.Data.Motion.Normal.Speed.X);
            var other = DirectionalCollisionRegistration.Get(world.Platforms)?.FirstOutside(world.SourcePlayer,
                world.Position + Vector2.UnitX * direction);
            if (other != null && other.AllowPositiveHorizontalMovement != (direction > 0))
                sequence.Data.Motion.Normal.Speed.X = 0f;
            return result;
        }

        public void Dispose()
        {
            for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
            hooks.Clear();
        }
    }
}
