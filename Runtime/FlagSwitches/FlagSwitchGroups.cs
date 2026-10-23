// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Diagnostics.Logging;
using Celeste.Game.Content.Maps;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.MaxHelpingHand.Content.Maps;
using Celeste.Mods.MaxHelpingHand.Contracts;
using Celeste.Mods.MaxHelpingHand.Entities.Switches;

namespace Celeste.Mods.MaxHelpingHand.Runtime.FlagSwitches
{
    internal sealed class FlagSwitchGroups
    {
        private readonly ISourceEngineContext engine;
        private readonly ConditionalWeakTable<OriginalChapterMapData, FlagSwitchMapGroups> maps = new();
        private readonly Dictionary<Type, DreamGateBinding> dreamGateBindings = new();

        public FlagSwitchGroups(ISourceEngineContext engine)
        {
            this.engine = engine;
        }

        internal void Set(OriginalChapterMapData map, FlagSwitchMapGroups groups)
        {
            maps.Remove(map);
            maps.Add(map, groups);
        }

        private FlagSwitchMapGroups Get(OriginalLevelSceneRoot level)
        {
            var map = (OriginalChapterMapData)level.Session.MapData;
            return maps.TryGetValue(map, out var groups)
                ? groups
                : throw new InvalidOperationException("Flag switch metadata was not processed for this map.");
        }

        internal void ResetFlagIfNecessary(OriginalLevelSceneRoot level, string flag, bool inverted)
        {
            var groups = Get(level);
            if (!Legacy(level, groups, flag, inverted) && !Persistent(level, groups, flag, inverted))
                level.Session.SetFlag(flag, inverted);
        }

        private bool Aggregate(OriginalLevelSceneRoot level, Dictionary<KeyValuePair<string, bool>, bool> dictionary,
            string name, string flag, bool inverted)
        {
            var key = new KeyValuePair<string, bool>(flag, inverted);
            if (dictionary.TryGetValue(key, out bool value)) return value;
            ((IEverestLoggerProcessEnvironment)engine).Logger.Warn("MaxHelpingHand/FlagTouchSwitch",
                $"Seems like there's a hole in the {name} dictionary ({((OriginalChapterMapData)level.Session.MapData).Area.SID} / {((OriginalChapterMapData)level.Session.MapData).Area.Mode} / {key})! Assuming true!");
            return true;
        }

        private bool Legacy(OriginalLevelSceneRoot level, FlagSwitchMapGroups groups, string flag, bool inverted) =>
            Aggregate(level, groups.LegacyModes, "FlagLegacyModes", flag, inverted);

        private bool Persistent(OriginalLevelSceneRoot level, FlagSwitchMapGroups groups, string flag, bool inverted) =>
            Aggregate(level, groups.Persistences, "FlagPersistences", flag, inverted) ||
            !groups.Gates.TryGetValue(flag, out var gates) || gates.Values.All(value => value);

        internal Func<object, IFlagMovingTouchSwitch> FindMovingSwitch;

        internal List<IFlagMovingTouchSwitch> CaptureMovingSwitches(OriginalLevelSceneRoot level, string flag)
        {
            var result = new List<IFlagMovingTouchSwitch>();
            foreach (var entity in level.SourceScene.Entities)
            {
                var moving = FindMovingSwitch?.Invoke(entity);
                if (moving != null && moving.Flag == flag) result.Add(moving);
            }

            return result;
        }

        internal bool Complete(OriginalLevelSceneRoot level, string flag, bool inverted, bool persistent, int id,
            List<FlagTouchSwitch> room, List<IFlagMovingTouchSwitch> moving, Action finished)
        {
            var groups = Get(level);
            if (persistent) level.Session.SetFlag(flag + "_switch" + id, true);
            if (!groups.Switches[new(flag, inverted)].All(switchId =>
                    switchId.Level == level.Session.Level || level.Session.GetFlag(flag + "_switch" + switchId.ID)) ||
                !room.All(touch => touch.Activated || touch.Hidden)) return false;
            if (!moving.All(touch => touch.Activated || touch.Hidden)) return false;
            foreach (var touch in room) touch.FinishGroup();
            foreach (var touch in moving) touch.Finish();
            finished();
            foreach (var gate in level.SourceScene.Entities.OfType<IFlagSwitchGate>())
                if (gate.Flag == flag)
                    gate.Trigger();
            foreach (var entity in level.SourceScene.Entities) TriggerDreamGate(entity, flag);
            if (groups.Gates.TryGetValue(flag, out var mapGates))
                foreach (var gate in mapGates)
                    if (gate.Value)
                        level.Session.SetFlag(flag + "_gate" + gate.Key.ID, true);
            if (!Legacy(level, groups, flag, inverted) || Persistent(level, groups, flag, inverted))
                level.Session.SetFlag(flag, !inverted);
            return true;
        }

        internal void ClearDreamGateBindings() => dreamGateBindings.Clear();

        private void TriggerDreamGate(object entity, string flag)
        {
            Type type = entity.GetType();
            if (!dreamGateBindings.TryGetValue(type, out var binding))
            {
                binding = DreamGateBinding.Create(type);
                dreamGateBindings.Add(type, binding);
            }

            binding?.Trigger(entity, flag);
        }
    }
}
