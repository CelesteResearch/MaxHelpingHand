using System;
using System.Numerics;
using System.Collections.Generic;
using System.Reflection;
using Celeste.Everest.Reflection.Runtime;
using Celeste.Mods.MaxHelpingHand.Entities;
using Celeste.Physics.Collision;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Player.Collision.Contacts;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Collision
{
    internal sealed partial class UpsideDownPlayerHooks
    {
        private Type movingCeilingType;

        private bool IsExcludedCeilingType(Type type)
            => type == typeof(UpsideDownJumpThru) || type == movingCeilingType;

        private void RegisterFloorQueries()
        {
            active.Add(new Hook(typeof(PlayerContacts).GetMethod(nameof(PlayerContacts.FloorJumpThruAt)),
                (Func<Func<PlayerContacts, Vector2, bool>, PlayerContacts, Vector2, bool>)FloorJumpThruAt,
                playerHookConfig));
            active.Add(new Hook(typeof(PlayerContacts).GetMethod(nameof(PlayerContacts.FloorJumpThruOutsideAt)),
                (Func<Func<PlayerContacts, Vector2, bool>, PlayerContacts, Vector2, bool>)FloorJumpThruOutsideAt,
                playerHookConfig));
            active.Add(new ILHook(typeof(RoomPlayerWorld).GetMethod(nameof(RoomPlayerWorld.GroundAtOnePixel)),
                PatchGround, playerHookConfig));
            active.Add(new ILHook(typeof(RoomPlayerWorld).GetMethod(nameof(RoomPlayerWorld.RefillGroundAtOnePixel)),
                PatchRefillGround, playerHookConfig));
        }

        private bool FloorJumpThruAt(Func<PlayerContacts, Vector2, bool> original, PlayerContacts contacts,
            Vector2 offset)
        {
            bool result = original(contacts, offset);
            var player = (RoomPlayerSession)contacts.ActualOwner;
            return result && (!Own(player) || !Ceiling(player, player.Position + offset));
        }

        private bool FloorJumpThruOutsideAt(Func<PlayerContacts, Vector2, bool> original, PlayerContacts contacts,
            Vector2 offset)
        {
            bool result = original(contacts, offset);
            var player = (RoomPlayerSession)contacts.ActualOwner;
            return result && (!Own(player) || !Ceiling(player, player.Position + offset, true));
        }

        private void PatchGround(ILContext il)
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After, IsJumpThruFirstOutside))
                throw new InvalidOperationException("The native ground jump-through query was not found.");
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<SourcePlatformEntityState, RoomPlayerWorld, SourcePlatformEntityState>>(
                SuppressGroundCeiling);
        }

        private SourcePlatformEntityState SuppressGroundCeiling(SourcePlatformEntityState platform,
            RoomPlayerWorld world)
        {
            if (platform == null || !Own(world.SourcePlayer)) return platform;
            return IsExcludedCeilingType(platform.GetType()) ? null : platform;
        }

        private void PatchRefillGround(ILContext il)
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After, IsJumpThruFirstOutside))
                throw new InvalidOperationException("The native refill jump-through query was not found.");
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<object, RoomPlayerWorld, object>>(SuppressRefillCeiling);
        }

        private object SuppressRefillCeiling(object platform, RoomPlayerWorld world)
            => platform != null && Own(world.SourcePlayer) &&
               Ceiling(world.SourcePlayer, world.SourcePlayer.Position + Vector2.UnitY, true)
                ? null
                : platform;

        private static bool IsJumpThruFirstOutside(Instruction instruction)
            => (instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
               instruction.Operand is GenericInstanceMethod method && method.Name == "FirstOutsideAt" &&
               method.DeclaringType.FullName == "Celeste.Scene.Entities.SourceTrackedEntityCollision";
    }
}
