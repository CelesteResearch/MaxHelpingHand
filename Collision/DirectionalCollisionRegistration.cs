using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Celeste.Mods.MaxHelpingHand.Collision
{
    public static class DirectionalCollisionRegistration
    {
        private static readonly Dictionary<object, RegistrationSet> Worlds = new(WorldIdentityComparer.Instance);

        public static DirectionalOneWayCollisionPolicy Get(object world)
            => world != null && Worlds.TryGetValue(world, out var set) ? set.Policy : null;

        public static IDisposable Register(object world, IDirectionalOneWayCollider collider)
        {
            if (!Worlds.TryGetValue(world, out var set))
                Worlds.Add(world, set = new RegistrationSet(world));
            return set.Add(collider);
        }

        private sealed class RegistrationSet
        {
            private readonly object world;
            private readonly List<IDirectionalOneWayCollider> colliders = new();
            public readonly DirectionalOneWayCollisionPolicy Policy;

            public RegistrationSet(object world)
            {
                this.world = world;
                Policy = new DirectionalOneWayCollisionPolicy(colliders);
            }

            public IDisposable Add(IDirectionalOneWayCollider collider)
            {
                colliders.Add(collider);

                return new Registration(this, collider);
            }

            public void Remove(IDirectionalOneWayCollider collider)
            {
                colliders.Remove(collider);

                if (colliders.Count == 0) Worlds.Remove(world);
            }
        }

        private sealed class Registration : IDisposable
        {
            private RegistrationSet owner;
            private readonly IDirectionalOneWayCollider collider;

            public Registration(RegistrationSet owner, IDirectionalOneWayCollider collider)
            {
                this.owner = owner;
                this.collider = collider;
            }

            public void Dispose()
            {
                var current = owner;
                if (current == null) return;
                owner = null;
                current.Remove(collider);
            }
        }

        private sealed class WorldIdentityComparer : IEqualityComparer<object>
        {
            public static readonly WorldIdentityComparer Instance = new();
            public new bool Equals(object left, object right) => ReferenceEquals(left, right);
            public int GetHashCode(object value) => RuntimeHelpers.GetHashCode(value);
        }
    }
}
