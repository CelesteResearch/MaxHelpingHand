using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Reflection.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.MaxHelpingHand.Triggers.Switches
{
    [CustomEntity("MaxHelpingHand/ActivateTimedTouchSwitchesTimerTrigger = Create")]
    [SceneTracked(false)]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Triggers.ActivateTimedTouchSwitchesTimerTrigger")]
    public class ActivateTimedTouchSwitchesTimerTrigger : SourceTriggerEntityState
    {
        private readonly ISourceEngineContext engine;
        private Dictionary<SceneEntityState, SimulationCoroutine> timedTouchSwitches;

        public static ActivateTimedTouchSwitchesTimerTrigger Create(OriginalLevelSceneRoot level,
            OriginalLevelRoomData room, Vector2 offset, OriginalLevelEntityData data) =>
            new(level.EngineContext, data, offset);

        public ActivateTimedTouchSwitchesTimerTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
                data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            this.engine = engine;
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            if (timedTouchSwitches != null) return;
            timedTouchSwitches = new Dictionary<SceneEntityState, SimulationCoroutine>();
            var scheduler = ((ISourceScene)scene).SourceScene;
            var entities = scheduler.Entities.Entities;
            var aliases = ((ISourceRuntimeTypeDiscoveryEnvironment)engine).RuntimeTypeDiscovery;
            for (int index = 0; index < entities.Count; index++)
            {
                var entity = (SceneEntityState)entities[index];
                if (aliases.RuntimeTypeNameMatches(entity.GetType(), "Celeste.Mod.OutbackHelper.TimedTouchSwitch"))
                {
                    var coroutine = entity.OwnedComponents.Get<SimulationCoroutine>();
                    if (coroutine != null)
                    {
                        entity.Remove(coroutine);
                        timedTouchSwitches.Add(entity, coroutine);
                    }
                }
            }

            var triggers = scheduler.Entities.GetTrackedEntities(typeof(ActivateTimedTouchSwitchesTimerTrigger));
            for (int index = 0; index < triggers.Count; index++)
                ((ActivateTimedTouchSwitchesTimerTrigger)triggers[index]).timedTouchSwitches = timedTouchSwitches;
        }

        internal void RestoreTimerOnTouchSwitch(SceneEntityState entity)
        {
            if (timedTouchSwitches != null && timedTouchSwitches.ContainsKey(entity))
            {
                entity.Add(timedTouchSwitches[entity]);
                timedTouchSwitches.Remove(entity);
            }
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            foreach (var pair in timedTouchSwitches) pair.Key.Add(pair.Value);
            var triggers = ((ISourceScene)Scene).SourceScene.Entities
                .GetTrackedEntities(typeof(ActivateTimedTouchSwitchesTimerTrigger));
            for (int index = 0; index < triggers.Count; index++)
                ((ActivateTimedTouchSwitchesTimerTrigger)triggers[index]).RemoveSelf();
        }
    }
}
