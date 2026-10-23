using Celeste.Physics.Collision;
using System.Numerics;

namespace Celeste.Mods.MaxHelpingHand.Collision
{
    public static class DirectionalEntityContact
    {
        public static bool SolidAt(bool original, ISourceEntityCollider mover, Vector2 at,
            DirectionalOneWayCollisionPolicy policy,
            DirectionalCollisionFeature feature = DirectionalCollisionFeature.Neutral)
            => original || mover.Position.X != at.X && policy?.EntityContact(mover, at, feature) != null;
    }
}
