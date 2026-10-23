using System;
using System.Numerics;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Riding;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Actors.Foundation;

namespace Celeste.Mods.MaxHelpingHand.Runtime.Platforms.Movement
{
    public static class SourceSideWallPlatformMovement
    {
        public static void Move(SceneEntityState platform, SourceSolidEntityState interactingSolid, bool left,
            Vector2 move)
        {
            if (platform.Scene == null)
            {
                platform.Position += move;
                interactingSolid.MoveHNaive(move.X);
                interactingSolid.MoveVNaive(move.Y);
                return;
            }

            var entities = ((ISourceScene)platform.Scene).SourceScene.Entities;
            bool carry = false;
            foreach (RoomPlayerSession player in entities.GetTrackedEntities(typeof(RoomPlayerSession)))
                if (OutsideAt(platform, player, platform.Position + move) && Math.Sign(move.X) == (left ? -1 : 1))
                {
                    carry = true;
                    break;
                }

            if (platform.Collidable && GetPlayerClimbing(platform, left) != null) carry = true;
            platform.Position += move;
            using (var riders = SourceSolidEntityState.CaptureMovementRiders())
            {
                interactingSolid.Collidable = carry;
                if (carry)
                    foreach (ISourcePlatformActor actor in entities.GetTrackedEntities(typeof(SourceActorEntityState)))
                        if (actor.IsRiding(interactingSolid))
                            riders.CarriedRiders.Add(actor);
                Vector2 liftSpeed = interactingSolid.LiftSpeed;
                interactingSolid.MoveH(move.X, liftSpeed.X);
                interactingSolid.MoveV(move.Y, liftSpeed.Y);
                interactingSolid.Collidable = false;
            }
        }

        public static RoomPlayerSession GetPlayerClimbing(SceneEntityState platform, bool left)
        {
            foreach (RoomPlayerSession player in ((ISourceScene)platform.Scene).SourceScene.Entities.GetTrackedEntities(
                         typeof(RoomPlayerSession)))
                if (player.Machine.State == 1 && ((!left && player.Facing == -1 &&
                                                   OutsideAt(platform, player, platform.Position + Vector2.UnitX)) ||
                                                  (left && player.Facing == 1 && OutsideAt(platform, player,
                                                      platform.Position - Vector2.UnitX))))
                    return player;
            return null;
        }

        private static bool OutsideAt(SceneEntityState platform, RoomPlayerSession player, Vector2 at)
            => !SourceEntityCollision.Check(platform, player) && SourceEntityCollision.CheckAt(platform, player, at);
    }
}
