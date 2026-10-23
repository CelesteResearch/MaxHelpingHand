using Celeste.Physics.Collision;
using System.Collections.Generic;
using System.Numerics;

namespace Celeste.Mods.MaxHelpingHand.Collision
{
    public sealed class DirectionalOneWayCollisionPolicy
    {
        private readonly IReadOnlyList<IDirectionalOneWayCollider> colliders;

        public DirectionalOneWayCollisionPolicy(IReadOnlyList<IDirectionalOneWayCollider> colliders)
            => this.colliders = colliders;

        public IDirectionalOneWayCollider FirstAt(ISourceEntityCollider mover, Vector2 at)
        {
            for (int index = 0; index < colliders.Count; index++)
            {
                var other = colliders[index];
                if (SourceEntityCollision.CheckAt(mover, other, at))
                    return other;
            }

            return null;
        }

        public T FirstAt<T>(ISourceEntityCollider mover, Vector2 at) where T : class, ISourceEntityCollider
        {
            for (int index = 0; index < colliders.Count; index++)
            {
                var other = colliders[index];
                if (other is T candidate && SourceEntityCollision.CheckAt(mover, candidate, at))
                    return candidate;
            }

            return null;
        }

        // Select the first geometric candidate BEFORE checking its permissions.
        // A permissive first candidate must not expose a blocking second candidate.
        public IDirectionalOneWayCollider FirstOutside(ISourceEntityCollider mover, Vector2 at)
        {
            for (int index = 0; index < colliders.Count; index++)
            {
                var other = colliders[index];
                if (!SourceEntityCollision.Check(mover, other) && SourceEntityCollision.CheckAt(mover, other, at))
                    return other;
            }

            return null;
        }

        public IDirectionalOneWayCollider Movement(ISourceEntityCollider mover, Vector2 at, int direction)
        {
            var other = FirstOutside(mover, at);
            return other != null && other.AllowPositiveHorizontalMovement != (direction > 0)
                                 && other.AllowsMovement(mover)
                ? other
                : null;
        }

        public IDirectionalOneWayCollider EntityContact(ISourceEntityCollider mover, Vector2 at,
            DirectionalCollisionFeature feature = DirectionalCollisionFeature.Neutral)
        {
            var other = FirstOutside(mover, at);
            return other != null && other.AllowsEntityContact(mover)
                                 && other.AllowPositiveHorizontalMovement == (mover.Position.X > at.X)
                                 && AllowsFeature(other, feature)
                                 && other.Collider.AbsoluteBottom >=
                                 mover.Collider.AbsoluteTop + at.Y - mover.Position.Y + 3
                ? other
                : null;
        }

        public IDirectionalOneWayCollider PointContact(Vector2 point,
            DirectionalCollisionFeature feature = DirectionalCollisionFeature.Neutral)
        {
            for (int index = 0; index < colliders.Count; index++)
            {
                var other = colliders[index];
                if (other.Collidable && other.Collider != null && other.Collider.Collide(point))
                    return AllowsFeature(other, feature) ? other : null;
            }

            return null;
        }

        private static bool AllowsFeature(IDirectionalOneWayCollider other, DirectionalCollisionFeature feature)
            => (feature != DirectionalCollisionFeature.Climb || other.AllowClimbing)
               && (feature != DirectionalCollisionFeature.WallJump || other.AllowWallJumping);
    }
}
