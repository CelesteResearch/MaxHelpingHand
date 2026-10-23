using System;
using Celeste.Everest.Reflection.Runtime;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Collectibles.Strawberries;
using Celeste.Mods.MaxHelpingHand.Entities.Collectibles.Strawberries.MultiRoom;
using Celeste.Mods.MaxHelpingHand.Modules;
using Celeste.Scene.Following;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Collectibles.Strawberry;
using Celeste.Simulation.Gameplay.Player.Intro;
using Mono.Cecil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System.Numerics;

namespace Celeste.Mods.MaxHelpingHand.Runtime.Collectibles.Strawberries.MultiRoom
{
    internal sealed class MultiRoomSeedHooks : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly MaxHelpingHandModule module;
        private readonly string playerRegistryKey;
        private readonly Func<ISourceStrawberrySeedFollower, bool> isNonPoppingSeed;
        private Hook loadLevel;
        private Hook returnRoutine;
        private ILHook climbHop;

        internal MultiRoomSeedHooks(ISourceEngineContext engine, MaxHelpingHandModule module)
        {
            this.engine = engine;
            this.module = module;
            playerRegistryKey = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog.Find(typeof(RoomPlayerSession))
                .RegistryKey;
            var nonPoppingSeed =
                ((ISourceRuntimeTypeDiscoveryEnvironment)engine).RuntimeTypeDiscovery.ResolveRuntimeTypeAlias(
                    "Celeste.Mod.MaxHelpingHand.Entities.NonPoppingStrawberrySeed");
            if (nonPoppingSeed == null) isNonPoppingSeed = _ => false;
            else
            {
                var method = new DynamicMethod("IsNonPoppingStrawberrySeed", typeof(bool),
                    new[] { typeof(ISourceStrawberrySeedFollower) }, typeof(MultiRoomSeedHooks), true);
                var code = method.GetILGenerator();
                code.Emit(System.Reflection.Emit.OpCodes.Ldarg_0);
                code.Emit(System.Reflection.Emit.OpCodes.Isinst, nonPoppingSeed);
                code.Emit(System.Reflection.Emit.OpCodes.Ldnull);
                code.Emit(System.Reflection.Emit.OpCodes.Cgt_Un);
                code.Emit(System.Reflection.Emit.OpCodes.Ret);
                isNonPoppingSeed =
                    (Func<ISourceStrawberrySeedFollower, bool>)method.CreateDelegate(
                        typeof(Func<ISourceStrawberrySeedFollower, bool>));
            }

            try
            {
                loadLevel = new Hook(typeof(OriginalLevelSceneRoot).GetMethod(nameof(OriginalLevelSceneRoot.LoadLevel)),
                    (Action<Action<OriginalLevelSceneRoot, PlayerIntroType, bool>, OriginalLevelSceneRoot,
                        PlayerIntroType, bool>)OnLoadLevel);
                climbHop = new ILHook(typeof(RoomPlayerWorld).GetMethod(nameof(RoomPlayerWorld.ClimbHopBlockedCheck)),
                    OnClimbHop);
                returnRoutine = new Hook(
                    typeof(OriginalStrawberrySeedState).GetMethod("ReturnRoutine",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    (Func<Func<OriginalStrawberrySeedState, IEnumerator>, OriginalStrawberrySeedState, IEnumerator>)
                    OnReturnRoutine);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void OnLoadLevel(Action<OriginalLevelSceneRoot, PlayerIntroType, bool> original,
            OriginalLevelSceneRoot level, PlayerIntroType intro, bool fromLoader)
        {
            original(level, intro, fromLoader);
            if (ReferenceEquals(level.EngineContext, engine) && intro != PlayerIntroType.Transition)
            {
                var player =
                    (RoomPlayerSession)((ISourceScene)level).SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>(
                        playerRegistryKey);
                if (player != null)
                {
                    Vector2 position = player.Position;
                    foreach (var info in module.Session.CollectedMultiRoomStrawberrySeeds)
                    {
                        position += new Vector2(-12 * player.Data.Facing, -8f);
                        level.SourceScene.QueueSourceEntity(new MultiRoomStrawberrySeed(player, position, info,
                            (IOriginalStrawberryProcess)engine));
                    }
                }
            }
        }

        private void OnClimbHop(ILContext il)
        {
            var cursor = new ILCursor(il);
            int count = 0;
            while (cursor.TryGotoNext(MoveType.Before, instruction => instruction.Operand is MethodReference method &&
                                                                      method.Name == "get_HasStrawberrySeedFollower" &&
                                                                      method.DeclaringType.FullName ==
                                                                      typeof(RoomPlayerWorld).FullName))
            {
                cursor.Remove();
                cursor.EmitDelegate<Func<RoomPlayerWorld, bool>>(world => HasBlockingSeed(world.SourcePlayer.Leader));
                count++;
            }

            if (count != 1)
                throw new InvalidOperationException("Multi-room seed climb-hop hook expected 1 site, found " + count +
                                                    ".");
        }

        private bool HasBlockingSeed(LeaderState leader)
        {
            if (!(leader.Entity is RoomPlayerSession player) || !(player.Scene is OriginalLevelSceneRoot level) ||
                !ReferenceEquals(level.EngineContext, engine))
                return leader.HasFollower<ISourceStrawberrySeedFollower>();
            foreach (var follower in leader.Followers)
                if (follower.Entity is ISourceStrawberrySeedFollower seed && !(seed is MultiRoomStrawberrySeed) &&
                    !isNonPoppingSeed(seed))
                    return true;
            return false;
        }

        private IEnumerator OnReturnRoutine(Func<OriginalStrawberrySeedState, IEnumerator> original,
            OriginalStrawberrySeedState seed)
        {
            if (!ReferenceEquals(StrawberrySeedFields.GetProcess(seed), engine)) return original(seed);
            return ReturnRoutine(original, seed);
        }

        private IEnumerator ReturnRoutine(Func<OriginalStrawberrySeedState, IEnumerator> original,
            OriginalStrawberrySeedState seed)
        {
            yield return new SourceSwapImmediately(original(seed));
            if (seed is MultiRoomStrawberrySeed multi &&
                ((OriginalLevelSceneRoot)seed.Scene).Session.Level != multi.StartingRoom) seed.RemoveSelf();
        }

        public void Dispose()
        {
            returnRoutine?.Dispose();
            returnRoutine = null;
            climbHop?.Dispose();
            climbHop = null;
            loadLevel?.Dispose();
            loadLevel = null;
        }
    }
}
