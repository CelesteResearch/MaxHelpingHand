// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Levels.Transitions;
using Celeste.Mods.MaxHelpingHand.Triggers.Camera;
using Celeste.Physics.Collision;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Camera;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Runtime.Camera
{
    public sealed class OneWayCameraHooks : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly List<ILHook> hooks = new List<ILHook>();
        private readonly string triggerKey;
        private readonly string playerKey;

        private readonly List<List<OneWayCameraTrigger>> collisionSnapshots = new List<List<OneWayCameraTrigger>>
            { new List<OneWayCameraTrigger>() };

        private int snapshotDepth;

        public OneWayCameraHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            var catalog = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog;
            triggerKey = catalog.Find(typeof(OneWayCameraTrigger)).Name;
            playerKey = catalog.Find(typeof(RoomPlayerSession)).Name;
            try
            {
                hooks.Add(new ILHook(typeof(RoomCamera).GetMethod(nameof(RoomCamera.Follow)), HookFollow));
                hooks.Add(new ILHook(typeof(OriginalLevelSceneRoot).GetMethod("LoadSourceRoomPlayer",
                    BindingFlags.Instance | BindingFlags.NonPublic), HookFullCameraTarget));
                var routine =
                    typeof(OriginalLevelTransitionSequence).GetMethod(nameof(OriginalLevelTransitionSequence
                        .CreateOriginalRoutine));
                var state = routine.GetCustomAttribute<IteratorStateMachineAttribute>().StateMachineType;
                hooks.Add(new ILHook(state.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.NonPublic),
                    context => HookTransition(context, state)));
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void HookFollow(ILContext context)
        {
            var cursor = new ILCursor(context);
            while (cursor.TryGotoNext(MoveType.After,
                       instruction => instruction.MatchCallOrCallvirt<ISourceCameraPlayer>("get_CameraTarget")))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<Vector2, RoomCamera, Vector2>>(ModifyFollowTarget);
            }
        }

        private void HookFullCameraTarget(ILContext context)
        {
            var cursor = new ILCursor(context);
            while (cursor.TryGotoNext(MoveType.After,
                       instruction =>
                           instruction.MatchCallOrCallvirt<OriginalLevelSceneRoot>(
                               nameof(OriginalLevelSceneRoot.GetFullCameraTargetAt))))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<Vector2, OriginalLevelSceneRoot, Vector2>>(ModifyLevelTarget);
            }
        }

        private void HookTransition(ILContext context, Type stateType)
        {
            var owner = stateType.GetField("<>4__this",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var cursor = new ILCursor(context);
            while (cursor.TryGotoNext(MoveType.After,
                       instruction =>
                           instruction.MatchCallOrCallvirt<OriginalLevelSceneRoot>(
                               nameof(OriginalLevelSceneRoot.GetFullCameraTargetAt))))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld, owner);
                cursor.Emit(OpCodes.Call,
                    typeof(OriginalLevelTransitionSequence).GetProperty(nameof(OriginalLevelTransitionSequence.Level))
                        .GetMethod);
                cursor.EmitDelegate<Func<Vector2, OriginalLevelSceneRoot, Vector2>>(ModifyLevelTarget);
            }
        }

        private Vector2 ModifyFollowTarget(Vector2 target, RoomCamera camera)
        {
            var level = engine.Frame.Scenes.CurrentScene as OriginalLevelSceneRoot;
            if (level == null || !ReferenceEquals(level.PlayerCamera, camera)) return target;
            return ClampCurrentScene(target, level);
        }

        private Vector2 ModifyLevelTarget(Vector2 target, OriginalLevelSceneRoot caller)
        {
            if (!ReferenceEquals(caller.EngineContext, engine)) return target;
            var level = engine.Frame.Scenes.CurrentScene as OriginalLevelSceneRoot;
            return level == null ? target : ClampCurrentScene(target, level);
        }

        private Vector2 ClampCurrentScene(Vector2 target, OriginalLevelSceneRoot level)
        {
            var entities = ((ISourceScene)level).SourceScene.Entities;
            var player = entities.GetTrackedEntity<RoomPlayerSession>(playerKey);
            if (player == null) return target;
            int depth = snapshotDepth++;
            if (depth == collisionSnapshots.Count) collisionSnapshots.Add(new List<OneWayCameraTrigger>());
            var snapshot = collisionSnapshots[depth];
            try
            {
                foreach (var entity in entities.GetTrackedEntities(triggerKey))
                {
                    var trigger = (OneWayCameraTrigger)entity;
                    if (SourceEntityCollision.Check(player, trigger)) snapshot.Add(trigger);
                }

                for (int i = 0; i < snapshot.Count; i++) target = snapshot[i].ClampTarget(target);
                return target;
            }
            finally
            {
                snapshot.Clear();
                snapshotDepth--;
            }
        }

        public void Dispose()
        {
            for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
            hooks.Clear();
        }
    }
}
