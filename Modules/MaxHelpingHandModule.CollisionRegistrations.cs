using System;
using System.Collections.Generic;
using Celeste.Mods.MaxHelpingHand.Collision;
using Celeste.Physics.Collision.Vertical;


namespace Celeste.Mods.MaxHelpingHand.Modules
{
    public sealed partial class MaxHelpingHandModule
    {
        private readonly HashSet<CollisionRegistration> collisionRegistrations = new();

        internal IDisposable RegisterDirectionalCollider(object world,
            IDirectionalOneWayCollider collider)
        {
            var registration =
                new CollisionRegistration(this, DirectionalCollisionRegistration.Register(world, collider));
            collisionRegistrations.Add(registration);
            return registration;
        }

        internal IDisposable RegisterVerticalCollider(IVerticalOneWayCollisionWorld world,
            IVerticalOneWayCollider collider)
        {
            var registration = new CollisionRegistration(this, VerticalCollisionRegistration.Register(world, collider));
            collisionRegistrations.Add(registration);
            return registration;
        }

        private void UnloadCollisionRegistrations()
        {
            foreach (var registration in new List<CollisionRegistration>(collisionRegistrations))
                registration.Dispose();
        }

        private sealed class CollisionRegistration : IDisposable
        {
            private MaxHelpingHandModule module;
            private readonly IDisposable registration;

            public CollisionRegistration(MaxHelpingHandModule module, IDisposable registration)
            {
                this.module = module;
                this.registration = registration;
            }

            public void Dispose()
            {
                var owner = module;
                if (owner == null) return;
                module = null;
                registration.Dispose();
                owner.collisionRegistrations.Remove(this);
            }
        }
    }
}
