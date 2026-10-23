using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using Celeste.Core.Scheduling;
using Celeste.Everest.Reflection.Runtime;
using Celeste.Mods.MaxHelpingHand.Entities;
using Celeste.Game.Gameplay.Enemies.Seekers;
using Celeste.Game.Execution.Levels;
using Celeste.Physics.Collision;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Platforms.Audio;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Collision
{
    internal sealed class PlatformCollisionHooks : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly List<IDisposable> hooks = new();
        private readonly string ceilingKey;
        private readonly Type movingCeilingType;

        public PlatformCollisionHooks(ISourceEngineContext engine, bool horizontal, bool vertical,
            DetourConfig playerRiderHookConfig = null)
        {
            this.engine = engine;
            if (vertical)
            {
                ceilingKey = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog.Find(typeof(UpsideDownJumpThru))
                    .RegistryKey;
                movingCeilingType = ((ISourceRuntimeTypeDiscoveryEnvironment)engine).RuntimeTypeDiscovery
                    .ResolveRuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.UpsideDownMovingPlatform");
            }

            try
            {
                if (horizontal)
                {
                    hooks.Add(new ILHook(typeof(SourcePlatformEntityState).GetMethod(
                        nameof(SourcePlatformEntityState.MoveHExactCollideSolids)), PatchHorizontal));
                    hooks.Add(new Hook(typeof(SourceSurfacePlatformSelection).GetMethod(
                            nameof(SourceSurfacePlatformSelection.Select)),
                        (Func<Func<List<object>, RoomPlayerSession, SourceSurfaceSoundSelection>, List<object>,
                            RoomPlayerSession, SourceSurfaceSoundSelection>)SelectSurface));
                    hooks.Add(new ILHook(typeof(OriginalSeekerState).GetMethod("OnCollideH",
                        BindingFlags.Instance | BindingFlags.NonPublic), PatchSeeker));
                }

                if (vertical)
                {
                    hooks.Add(new ILHook(typeof(SourcePlatformEntityState).GetMethod(
                        nameof(SourcePlatformEntityState.MoveVExactCollideSolids)), PatchVertical));
                    hooks.Add(new Hook(typeof(SourceJumpThruEntityState).GetMethod(
                            nameof(SourceJumpThruEntityState.HasPlayerRider)),
                        (Func<Func<SourceJumpThruEntityState, bool>, SourceJumpThruEntityState, bool>)HasPlayerRider,
                        playerRiderHookConfig));
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private bool Own(object scene) =>
            scene is OriginalLevelSceneRoot level && ReferenceEquals(level.EngineContext, engine);

        private static bool TrackedCall(Instruction instruction, string name)
            => instruction.Operand is MethodReference method && method.Name == name &&
               method.DeclaringType.FullName == typeof(SourceTrackedEntityCollision).FullName;

        private void PatchHorizontal(ILContext il)
        {
            var policy = new VariableDefinition(il.Import(typeof(DirectionalOneWayCollisionPolicy)));
            il.Body.Variables.Add(policy);
            var cursor = new ILCursor(il);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<SourcePlatformEntityState, DirectionalOneWayCollisionPolicy>>(HorizontalPolicy);
            cursor.Emit(OpCodes.Stloc, policy);
            cursor.GotoNext(MoveType.After, instruction => TrackedCall(instruction, "FirstAt"));
            var unchanged = cursor.DefineLabel();
            cursor.Emit(OpCodes.Ldloc, policy);
            cursor.Emit(OpCodes.Brfalse, unchanged);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.Emit(OpCodes.Ldloc, policy);
            cursor.EmitDelegate<Func<SourceSolidEntityState, SourcePlatformEntityState, int,
                DirectionalOneWayCollisionPolicy, ISourcePlatformEntity>>(HorizontalCandidate);
            cursor.MarkLabel(unchanged);
        }

        private DirectionalOneWayCollisionPolicy HorizontalPolicy(SourcePlatformEntityState platform)
            => Own(platform.Scene) ? DirectionalCollisionRegistration.Get(platform.Mover.World) : null;

        private static ISourcePlatformEntity HorizontalCandidate(SourceSolidEntityState original,
            SourcePlatformEntityState platform, int move, DirectionalOneWayCollisionPolicy policy)
        {
            if (original != null) return original;
            int direction = Math.Sign(move);
            var hit = policy.Movement(platform,
                platform.Position + Vector2.UnitX * direction, direction);
            return hit?.GetMovementCollisionTarget(platform) as ISourcePlatformEntity;
        }

        private void PatchVertical(ILContext il)
        {
            var policy = new VariableDefinition(il.Import(typeof(List<object>)));
            il.Body.Variables.Add(policy);
            var cursor = new ILCursor(il);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<SourcePlatformEntityState, List<object>>>(VerticalCandidates);
            cursor.Emit(OpCodes.Stloc, policy);
            cursor.GotoNext(MoveType.After, instruction => TrackedCall(instruction, "FirstAt"));
            var unchangedSolid = cursor.DefineLabel();
            cursor.Emit(OpCodes.Ldloc, policy);
            cursor.Emit(OpCodes.Brfalse, unchangedSolid);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.Emit(OpCodes.Ldloc, policy);
            cursor.EmitDelegate<Func<SourceSolidEntityState, SourcePlatformEntityState, int,
                List<object>, SourcePlatformEntityState>>(VerticalCandidate);
            cursor.MarkLabel(unchangedSolid);
            cursor.GotoNext(MoveType.After, instruction => TrackedCall(instruction, "FirstOutsideAt"));
            var unchangedJumpThru = cursor.DefineLabel();
            cursor.Emit(OpCodes.Ldloc, policy);
            cursor.Emit(OpCodes.Brfalse, unchangedJumpThru);
            cursor.Emit(OpCodes.Ldloc, policy);
            cursor.EmitDelegate<Func<SourcePlatformEntityState, List<object>,
                SourcePlatformEntityState>>(FilterCeiling);
            cursor.MarkLabel(unchangedJumpThru);
        }

        private List<object> VerticalCandidates(SourcePlatformEntityState platform)
            => Own(platform.Scene)
                ? ((ISourceScene)platform.Scene).SourceScene.Entities.GetTrackedEntities(ceilingKey)
                : null;

        private static SourcePlatformEntityState VerticalCandidate(SourceSolidEntityState original,
            SourcePlatformEntityState platform, int move, List<object> candidates)
        {
            if (original != null || move >= 0)
                return original;
            for (int index = 0; index < candidates.Count; index++)
            {
                if (candidates[index] is not UpsideDownJumpThru ceiling) continue;
                if (!SourceEntityCollision.Check(platform, ceiling) &&
                    SourceEntityCollision.CheckAt(platform, ceiling, platform.Position - Vector2.UnitY)) return ceiling;
            }

            return null;
        }

        private SourcePlatformEntityState FilterCeiling(SourcePlatformEntityState original,
            List<object> candidates)
        {
            if (original != null && IsExcludedCeilingType(original.GetType())) return null;
            return original;
        }

        private bool IsExcludedCeilingType(Type type)
            => type == typeof(UpsideDownJumpThru) || type == movingCeilingType;

        private bool HasPlayerRider(Func<SourceJumpThruEntityState, bool> original, SourceJumpThruEntityState platform)
            => (!Own(platform.Scene) || platform is not UpsideDownJumpThru) && original(platform);

        private SourceSurfaceSoundSelection SelectSurface(
            Func<List<object>, RoomPlayerSession, SourceSurfaceSoundSelection> original,
            List<object> platforms, RoomPlayerSession player)
        {
            if (platforms.Count == 0 && player != null && ReferenceEquals(player.EngineContext, engine))
            {
                var hit = DirectionalCollisionRegistration.Get(player.Body.World)?.FirstAt<IWallSurfaceSoundCollider>(
                    player, player.Center + Vector2.UnitX * player.Data.Facing);
                if (hit != null && hit.WallSurfaceSoundIndex != -1)
                    return new SourceSurfaceSoundSelection(hit.WallSurfaceSoundIndex);
            }

            return original(platforms, player);
        }

        private void PatchSeeker(ILContext il)
        {
            var at = new VariableDefinition(il.Import(typeof(Vector2)));
            il.Body.Variables.Add(at);
            var cursor = new ILCursor(il);
            int count = 0;
            while (cursor.TryGotoNext(MoveType.Before, instruction => instruction.Operand is MethodReference method &&
                                                                      method.Name ==
                                                                      nameof(SceneEntityState.CollideCheck) &&
                                                                      method.Parameters.Count == 2 &&
                                                                      method.Parameters[0].ParameterType.FullName ==
                                                                      typeof(string).FullName &&
                                                                      method.Parameters[1].ParameterType.FullName ==
                                                                      typeof(Vector2).FullName))
            {
                cursor.Emit(OpCodes.Dup);
                cursor.Emit(OpCodes.Stloc, at);
                cursor.Index++;
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloc, at);
                cursor.EmitDelegate<Func<bool, OriginalSeekerState, Vector2, bool>>(SeekerContact);
                count++;
            }

            if (count != 2) throw new InvalidOperationException("Expected both Seeker.OnCollideH solid probes.");
        }

        private bool SeekerContact(bool original, OriginalSeekerState seeker, Vector2 at)
        {
            if (original || !Own(seeker.Scene) || seeker.Position.X == at.X) return original;
            var world = (seeker.Scene as ISourcePlatformCollisionScene)?.PlatformWorld;
            return DirectionalCollisionRegistration.Get(world)?.EntityContact(seeker, at) != null;
        }

        public void Dispose()
        {
            for (int index = hooks.Count - 1; index >= 0; index--) hooks[index].Dispose();
            hooks.Clear();
        }
    }
}
