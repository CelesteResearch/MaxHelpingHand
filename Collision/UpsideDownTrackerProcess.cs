using System;
using System.Collections.Generic;
using System.Reflection;
using Celeste.Core.Reflection.Runtime;
using Celeste.Mods.MaxHelpingHand.Entities;
using Celeste.Physics.Collision.Vertical;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Collision
{
    internal sealed class UpsideDownTrackerProcess : IDisposable
    {
        private readonly Hook dashHook;
        private readonly Hook redDashHook;
        private readonly Dictionary<Type, bool> excludedTypes = new();

        public UpsideDownTrackerProcess()
        {
            dashHook = new Hook(typeof(RoomPlayerWorld).GetMethod(nameof(RoomPlayerWorld.CorrectJumpThrus)),
                (Action<Action<RoomPlayerWorld>, RoomPlayerWorld>)CorrectJumpThrus);
            try
            {
                redDashHook = new Hook(
                    typeof(RoomPlayerWorld).GetMethod(nameof(RoomPlayerWorld.RedDashCorrectJumpThrus)),
                    (Action<Action<RoomPlayerWorld>, RoomPlayerWorld>)CorrectJumpThrus);
            }
            catch
            {
                dashHook.Dispose();
                throw;
            }
        }

        private void CorrectJumpThrus(Action<RoomPlayerWorld> original, RoomPlayerWorld world)
        {
            var policy = world.Platforms.VerticalCollisionPolicy;
            if (policy != null)
            {
                var player = (RoomPlayerSession)world.Body.SourceColliderOwner;
                var registry = ((ISourceScene)player.Scene).SourceScene.Entities;
                FilterLiveList(registry.GetTrackedEntities("Celeste.JumpThru"), policy);
            }

            original(world);
        }

        // This is intentionally the upstream live-list mutation, at the exact two
        // horizontal dash query points. Other tracked types and scene entities stay intact.
        internal void FilterLiveList(List<object> matches, VerticalOneWayCollisionPolicy policy)
        {
            for (var index = 0; index < matches.Count; index++)
            {
                var entity = matches[index];
                if (entity is IVerticalOneWayCollider { AllowPositiveVerticalMovement: true } ceiling
                    && policy.IsRegistered(ceiling) && IsExcludedType(entity.GetType()))
                {
                    matches.RemoveAt(index);
                    index--;
                }
            }
        }

        private bool IsExcludedType(Type type)
        {
            if (type == typeof(UpsideDownJumpThru)) return true;
            if (excludedTypes.TryGetValue(type, out var excluded)) return excluded;
            excluded = false;
            foreach (var alias in type.GetCustomAttributes<RuntimeTypeAliasAttribute>(false))
            {
                if (alias.Name != "Celeste.Mod.MaxHelpingHand.Entities.UpsideDownMovingPlatform") continue;
                excluded = true;
                break;
            }

            excludedTypes.Add(type, excluded);
            return excluded;
        }

        public void Dispose()
        {
            redDashHook.Dispose();
            dashHook.Dispose();
            excludedTypes.Clear();
            // Upstream deactivation does not reinsert entries into this scene's tracker.
        }
    }
}
