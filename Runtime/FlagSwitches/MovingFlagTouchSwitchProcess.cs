// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Updating;
using Celeste.Everest.Modules.Runtime;
using Celeste.Game.Components.Switches;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.MaxHelpingHand.Contracts;
using Celeste.Game.Gameplay.Mechanisms.Switches;
using Celeste.Scene.Entities;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Runtime.FlagSwitches
{
    internal sealed class MovingFlagTouchSwitchProcess : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly FlagSwitchGroups groups;
        private readonly Dictionary<SceneEntityState, MovingFlagTouchSwitchAttachment> mapping = new();
        private Hook activation, removed, ended;
        private static readonly Action<OriginalSwitchComponent, bool> SetActivated = BindActivated();
        private MovingFlagTouchSwitchBinding binding;
        internal MovingFlagTouchSwitchBinding Binding => binding ??= new MovingFlagTouchSwitchBinding(engine);

        public MovingFlagTouchSwitchProcess(ISourceEngineContext engine, FlagSwitchGroups groups)
        {
            this.engine = engine;
            this.groups = groups;
            groups.FindMovingSwitch = Find;
        }

        internal void HookMods()
        {
            if (activation != null ||
                !((IEverestLoaderProcessEnvironment)engine).Loader.DependencyLoaded(new EverestModuleMetadata
                    { Name = "OutbackHelper", Version = new Version(1, 4, 0) })) return;
            _ = Binding;
            activation = new Hook(typeof(OriginalSwitchComponent).GetMethod(nameof(OriginalSwitchComponent.Activate)),
                (Func<Func<OriginalSwitchComponent, bool>, OriginalSwitchComponent, bool>)Activate);
            removed = new Hook(typeof(SceneEntityState).GetMethod(nameof(SceneEntityState.Removed)),
                (Action<Action<SceneEntityState, object>, SceneEntityState, object>)Removed);
            ended = new Hook(typeof(OriginalLevelSceneRoot).GetMethod(nameof(OriginalLevelSceneRoot.End)),
                (Action<Action<OriginalLevelSceneRoot>, OriginalLevelSceneRoot>)End);
        }

        internal void Register(OriginalTouchSwitchState moving, MovingFlagTouchSwitchAttachment attachment)
        {
            mapping[moving] = attachment;
            attachment.Registered = true;
        }

        private IFlagMovingTouchSwitch Find(object entity) =>
            entity is SceneEntityState state && mapping.TryGetValue(state, out var attachment) ? attachment : null;

        private bool Activate(Func<OriginalSwitchComponent, bool> original, OriginalSwitchComponent self)
        {
            if (self.Entity is OriginalTouchSwitchState moving && mapping.TryGetValue(moving, out var attachment))
            {
                if (self.Finished || self.Activated) return false;
                SetActivated(self, true);
                self.OnActivate?.Invoke();
                return attachment.Complete();
            }

            return original(self);
        }

        private void Removed(Action<SceneEntityState, object> original, SceneEntityState self, object scene)
        {
            original(self, scene);
            if (mapping.TryGetValue(self, out var attachment)) attachment.Registered = false;
            mapping.Remove(self);
        }

        private void End(Action<OriginalLevelSceneRoot> original, OriginalLevelSceneRoot level)
        {
            original(level);
            if (ReferenceEquals(level.EngineContext, engine)) ClearMapping();
        }

        public void Dispose()
        {
            ended?.Dispose();
            removed?.Dispose();
            activation?.Dispose();
            groups.FindMovingSwitch = null;
            ClearMapping();
        }

        private void ClearMapping()
        {
            foreach (var attachment in mapping.Values) attachment.Registered = false;
            mapping.Clear();
        }

        private static Action<OriginalSwitchComponent, bool> BindActivated()
        {
            var setter = typeof(OriginalSwitchComponent).GetProperty(nameof(OriginalSwitchComponent.Activated))
                .GetSetMethod(true);
            var method = new DynamicMethod("SetSwitchActivated", typeof(void),
                new[] { typeof(OriginalSwitchComponent), typeof(bool) }, typeof(MovingFlagTouchSwitchProcess).Module,
                true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, setter);
            il.Emit(OpCodes.Ret);
            return (Action<OriginalSwitchComponent, bool>)method.CreateDelegate(
                typeof(Action<OriginalSwitchComponent, bool>));
        }
    }
}
