using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Everest.Reflection.Runtime;
using Celeste.Game.Content.Maps;
using Celeste.Game.Execution.LevelLoader;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Platforms.CrushBlocks;
using Celeste.Mods.MaxHelpingHand.Entities.Barriers;
using Celeste.Physics.Collision;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.Crushing;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Runtime.Barriers
{
    public sealed class KevinBarrierHooks : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly List<IDisposable> hooks = new();
        private readonly HashSet<Type> hookedTypes = new();
        private readonly Action<Vector2, Vector2, ISourcePlatformEntity> notifyHit;
        private bool barriersCollidable;

        private const BindingFlags InstanceMethods =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public KevinBarrierHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            notifyHit = NotifyHit;
            try
            {
                hooks.Add(new Hook(typeof(OriginalLevelLoaderScene).GetMethod("LoadingThread", InstanceMethods),
                    (Action<Action<OriginalLevelLoaderScene>, OriginalLevelLoaderScene>)LoadingThread));
                hooks.Add(new Hook(typeof(PixelActor).GetMethod(nameof(PixelActor.MoveHExact)),
                    (Func<Func<PixelActor, int, SourceCollisionHandler, ISourceEntityCollider, bool>, PixelActor, int,
                        SourceCollisionHandler, ISourceEntityCollider, bool>)ActorMoveHorizontal));
                hooks.Add(new Hook(typeof(PixelActor).GetMethod(nameof(PixelActor.MoveVExact)),
                    (Func<Func<PixelActor, int, SourceCollisionHandler, ISourceEntityCollider, bool>, PixelActor, int,
                        SourceCollisionHandler, ISourceEntityCollider, bool>)ActorMoveVertical));
                HookKevin(typeof(OriginalCrushBlockState));
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void HookMods()
        {
            var discovery = ((ISourceRuntimeTypeDiscoveryEnvironment)engine).RuntimeTypeDiscovery;
            foreach (string name in new[]
                     {
                         "FrostHelper.CustomCrushBlock", "Celeste.Mod.CherryHelper.NonReturnCrushBlock",
                         "Celeste.Mod.CherryHelper.UninterruptedNRCB", "Celeste.Mod.CherryHelper.NonReturnSokoban",
                         "Celeste.Mod.Sardine7.Entities.SokobanBlock"
                     })
                if (discovery.ResolveRuntimeTypeAlias(name) is Type type)
                    HookKevin(type);
        }

        private void HookKevin(Type type)
        {
            if (!hookedTypes.Add(type)) return;
            MethodInfo horizontal = type.GetMethod("MoveHCheck", InstanceMethods),
                vertical = type.GetMethod("MoveVCheck", InstanceMethods);
            if (horizontal == null && vertical == null && typeof(OriginalCrushBlockState).IsAssignableFrom(type))
                return; // Its movement uses the already hooked shared CrushBlock methods.
            if (horizontal == null || vertical == null)
                throw new MissingMethodException(type.FullName, horizontal == null ? "MoveHCheck" : "MoveVCheck");
            if (typeof(ICrushingSolidCollisionCallbackSource).IsAssignableFrom(type))
            {
                hooks.Add(new Hook(horizontal,
                    (Func<Func<SourceSolidEntityState, float, bool>, SourceSolidEntityState, float, bool>)
                    MoveHorizontalWithCallback));
                hooks.Add(new Hook(vertical,
                    (Func<Func<SourceSolidEntityState, float, bool>, SourceSolidEntityState, float, bool>)
                    MoveVerticalWithCallback));
            }
            else
            {
                hooks.Add(new Hook(horizontal,
                    (Func<Func<SourceSolidEntityState, float, bool>, SourceSolidEntityState, float, bool>)
                    MoveHorizontal));
                hooks.Add(new Hook(vertical,
                    (Func<Func<SourceSolidEntityState, float, bool>, SourceSolidEntityState, float, bool>)
                    MoveVertical));
                hooks.Add(new ILHook(horizontal, PatchCollideCallback));
                hooks.Add(new ILHook(vertical, PatchCollideCallback));
            }
        }

        private void LoadingThread(Action<OriginalLevelLoaderScene> orig, OriginalLevelLoaderScene loader)
        {
            if (ReferenceEquals(loader.EngineContext, engine) && loader.Session.MapData is OriginalChapterMapData map)
            {
                bool hasBarrier = false;
                foreach (var room in map.Levels)
                {
                    if (room.Entities == null) continue;
                    foreach (var entity in room.Entities)
                        if (entity.Name == "MaxHelpingHand/KevinBarrier")
                        {
                            hasBarrier = true;
                            break;
                        }

                    if (hasBarrier) break;
                }

                if (hasBarrier) loader.Level.AddEntity(new KevinBarrierRenderer(engine));
            }

            orig(loader);
        }

        private bool Owns(object scene) =>
            scene is ISourceEngineContextScene owner && ReferenceEquals(owner.EngineContext, engine);

        private static List<object> Barriers(object scene) =>
            ((ISourceScene)scene).SourceScene.Entities.GetTrackedEntities(typeof(KevinBarrier));

        private static void SetCollidable(List<object> barriers, bool value)
        {
            for (int i = 0; i < barriers.Count; i++) ((KevinBarrier)barriers[i]).Collidable = value;
        }

        private bool MoveHorizontalWithCallback(Func<SourceSolidEntityState, float, bool> orig,
            SourceSolidEntityState self, float amount)
        {
            if (!Owns(self.Scene)) return orig(self, amount);
            var source = (ICrushingSolidCollisionCallbackSource)self;
            var previous = source.CrushingCollisionCallback;
            source.CrushingCollisionCallback = WrapCallback(previous);
            try
            {
                return MoveHorizontal(orig, self, amount);
            }
            finally
            {
                source.CrushingCollisionCallback = previous;
            }
        }

        private bool MoveVerticalWithCallback(Func<SourceSolidEntityState, float, bool> orig,
            SourceSolidEntityState self, float amount)
        {
            if (!Owns(self.Scene)) return orig(self, amount);
            var source = (ICrushingSolidCollisionCallbackSource)self;
            var previous = source.CrushingCollisionCallback;
            source.CrushingCollisionCallback = WrapCallback(previous);
            try
            {
                return MoveVertical(orig, self, amount);
            }
            finally
            {
                source.CrushingCollisionCallback = previous;
            }
        }

        private bool MoveHorizontal(Func<SourceSolidEntityState, float, bool> orig, SourceSolidEntityState self,
            float amount)
        {
            if (!Owns(self.Scene)) return orig(self, amount);
            barriersCollidable = true;
            var barriers = Barriers(self.Scene);
            SetCollidable(barriers, true);
            bool hit = orig(self, amount);
            if (hit) self.MoveHCollideSolidsAndBounds((OriginalLevelSceneRoot)self.Scene, amount, true, notifyHit);
            barriersCollidable = false;
            SetCollidable(barriers, false);
            return hit;
        }

        private bool MoveVertical(Func<SourceSolidEntityState, float, bool> orig, SourceSolidEntityState self,
            float amount)
        {
            if (!Owns(self.Scene)) return orig(self, amount);
            barriersCollidable = true;
            var barriers = Barriers(self.Scene);
            SetCollidable(barriers, true);
            bool hit = orig(self, amount);
            barriersCollidable = false;
            SetCollidable(barriers, false);
            return hit;
        }

        private void PatchCollideCallback(ILContext il)
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(instruction =>
                    instruction.MatchCall<SourcePlatformEntityState>(nameof(SourcePlatformEntityState
                        .MoveVCollideSolidsAndBounds))
                    || instruction.MatchCall<SourcePlatformEntityState>(nameof(SourcePlatformEntityState
                        .MoveHCollideSolidsAndBounds))))
                throw new InvalidOperationException("Kevin collision call not found: " + il.Method.FullName);
            if (!cursor.TryGotoPrev(MoveType.After, instruction => instruction.MatchLdnull()))
                throw new InvalidOperationException("Kevin collision callback not found: " + il.Method.FullName);
            cursor
                .EmitDelegate<
                    Func<Action<Vector2, Vector2, ISourcePlatformEntity>,
                        Action<Vector2, Vector2, ISourcePlatformEntity>>>(WrapCallback);
        }

        private Action<Vector2, Vector2, ISourcePlatformEntity> WrapCallback(
            Action<Vector2, Vector2, ISourcePlatformEntity> orig)
        {
            if (orig == null) return notifyHit;
            return (a, b, platform) =>
            {
                if (platform is KevinBarrier barrier && Owns(barrier.Scene)) barrier.HitByKevin();
                else orig(a, b, platform);
            };
        }

        private void NotifyHit(Vector2 a, Vector2 b, ISourcePlatformEntity platform)
        {
            if (platform is KevinBarrier barrier && Owns(barrier.Scene)) barrier.HitByKevin();
        }

        private bool ActorMoveHorizontal(
            Func<PixelActor, int, SourceCollisionHandler, ISourceEntityCollider, bool> orig,
            PixelActor self, int amount, SourceCollisionHandler onCollide, ISourceEntityCollider pusher)
        {
            if (!barriersCollidable || self.SourceColliderOwner is not ISourceEntityScene owner || !Owns(owner.Scene))
                return orig(self, amount, onCollide, pusher);
            var barriers = Barriers(owner.Scene);
            SetCollidable(barriers, false);
            bool result = orig(self, amount, onCollide, pusher);
            SetCollidable(barriers, true);
            return result;
        }

        private bool ActorMoveVertical(Func<PixelActor, int, SourceCollisionHandler, ISourceEntityCollider, bool> orig,
            PixelActor self, int amount, SourceCollisionHandler onCollide, ISourceEntityCollider pusher)
        {
            if (!barriersCollidable || self.SourceColliderOwner is not ISourceEntityScene owner || !Owns(owner.Scene))
                return orig(self, amount, onCollide, pusher);
            var barriers = Barriers(owner.Scene);
            SetCollidable(barriers, false);
            bool result = orig(self, amount, onCollide, pusher);
            SetCollidable(barriers, true);
            return result;
        }

        public void Dispose()
        {
            for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
            hooks.Clear();
            hookedTypes.Clear();
        }
    }
}
