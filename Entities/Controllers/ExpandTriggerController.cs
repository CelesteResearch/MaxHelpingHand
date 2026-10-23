// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.MaxHelpingHand.Entities.Controllers
{
    [CustomEntity("MaxHelpingHand/ExpandTriggerController")]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.ExpandTriggerController")]
    public class ExpandTriggerController : SceneEntityState
    {
        public ExpandTriggerController(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            Collider = new HitboxColliderGeometry(24f, 24f, -12f, -12f);
        }

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, EntityData data) => new ExpandTriggerController(level.EngineContext, data, offset);

        public override void Awake(object scene)
        {
            base.Awake(scene);
            // Entity.CollideAll<Trigger> takes a snapshot of the tracked Trigger bucket before any mutation.
            var overlaps = new List<SourceTriggerEntityState>();
            var triggers = ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities("Celeste.Trigger");
            for (int i = 0; i < triggers.Count; i++)
            {
                var trigger = (SourceTriggerEntityState)triggers[i];
                if (SourceEntityCollision.Check(this, trigger)) overlaps.Add(trigger);
            }

            foreach (var trigger in overlaps)
            {
                trigger.Position = new Vector2(((OriginalLevelSceneRoot)Scene).Bounds.Left,
                    ((OriginalLevelSceneRoot)Scene).Bounds.Top - 24f);
                trigger.Collider.Width = ((OriginalLevelSceneRoot)Scene).Bounds.Width;
                trigger.Collider.Height = ((OriginalLevelSceneRoot)Scene).Bounds.Height + 32f;
            }

            RemoveSelf();
        }
    }
}
