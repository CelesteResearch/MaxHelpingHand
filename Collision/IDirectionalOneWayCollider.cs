using Celeste.Physics.Collision;

namespace Celeste.Mods.MaxHelpingHand.Collision
{
    public interface IDirectionalOneWayCollider : ISourceEntityCollider
    {
        bool AllowPositiveHorizontalMovement { get; }
        bool AllowClimbing { get; }

        bool AllowWallJumping { get; }

        // True means this mover participates in movement blocking (not permission to pass).
        bool AllowsMovement(ISourceEntityCollider mover);
        bool AllowsEntityContact(ISourceEntityCollider mover);
        ISourceEntityCollider GetMovementCollisionTarget(ISourceEntityCollider mover);
    }
}
