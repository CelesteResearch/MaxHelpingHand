using System;
using System.Numerics;
using Celeste.Physics.Collision;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Player.Collision.Contacts;
using Celeste.Simulation.Gameplay.Player.Collision.Responses;
using Celeste.Simulation.Gameplay.Player.Movement;
using Celeste.Simulation.Gameplay.Player.State;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace Celeste.Mods.MaxHelpingHand.Collision
{
    internal sealed partial class UpsideDownPlayerHooks
    {
        private void Update(Action<RoomPlayerSession> original, RoomPlayerSession player)
        {
            if (!Own(player))
            {
                original(player);
                return;
            }

            var contacts = player.World.Contacts;
            bool blocked = player.Data.Motion.Ducking && !Ceiling(player);
            if (blocked)
            {
                var saved = contacts.Collider;
                contacts.SetCollider(contacts.NormalHitbox);
                blocked = Ceiling(player);
                contacts.SetCollider(saved);
            }

            original(player);
            if (blocked && !player.Data.Motion.Ducking)
                while (Ceiling(player) && !contacts.SolidAt(Vector2.UnitY))
                    player.Position += Vector2.UnitY;
        }

        private bool CanUnDuck(Func<PlayerContacts, bool> original, PlayerContacts contacts)
        {
            bool result = original(contacts);
            var player = (RoomPlayerSession)contacts.ActualOwner;
            if (!Own(player) || !result || !player.Data.Motion.Ducking || Ceiling(player)) return result;
            var saved = contacts.Collider;
            float y = player.Position.Y;
            contacts.SetCollider(contacts.NormalHitbox);
            if (Ceiling(player))
            {
                while (Ceiling(player) && !contacts.SolidAt(Vector2.UnitY) &&
                       !player.Body.CollideJumpThruOutside(player.Position + Vector2.UnitY))
                    player.Position += Vector2.UnitY;
                result = !Ceiling(player);
            }

            contacts.SetCollider(saved);
            player.Position = new Vector2(player.Position.X, y);
            return result;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private void Vertical(Action<PlayerCollisionResponse, PixelCollision, SourcePlatformEntityState> original,
            PlayerCollisionResponse response, PixelCollision collision, SourcePlatformEntityState hit)
        {
            var player = response.Player;
            if (Own(player) && player.Machine.State != 19 && player.Machine.State != 3 && player.Machine.State != 9 &&
                player.Data.Motion.Normal.Speed.Y < 0f && Ceiling(player, player.Position - Vector2.UnitY, true))
            {
                player.Data.Motion.Normal.Speed.Y = 0f;
                player.Data.Motion.Normal.VariableJumpTimer = 0f;
            }

            original(response, collision, hit);
        }

        private void PatchClimb(ILContext il)
        {
            var cursor = new ILCursor(il) { Index = il.Body.Instructions.Count };
            cursor.GotoPrev(MoveType.Before, i => i.MatchLdfld<PlayerState>(nameof(PlayerState.ClimbNoMoveTimer)));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<ClimbSequence>>(Climb);
        }

        private void Climb(ClimbSequence sequence)
        {
            if (!ReferenceEquals(sequence.EngineContext, engine) || sequence.World is not RoomPlayerWorld world) return;
            var player = world.SourcePlayer;
            if (player.Input.MoveY.Value == -1 && Ceiling(player, player.Position - Vector2.UnitY, true))
            {
                sequence.Data.Motion.Normal.Speed.Y = 0f;
                sequence.Data.LastClimbMove = 0;
            }
        }
    }
}
