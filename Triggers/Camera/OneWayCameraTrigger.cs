// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Core.Reflection.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Entities.Hazards.Killboxes;
using Celeste.Game.Entities.Platforms.Barriers;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.MaxHelpingHand.Triggers.Camera
{
    [CustomEntity("MaxHelpingHand/OneWayCameraTrigger")]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Triggers.OneWayCameraTrigger")]
    [SceneTracked(false)]
    public class OneWayCameraTrigger : SourceTriggerEntityState
    {
        private readonly bool left, right, up, down, blockPlayer;
        private readonly string flag;
        private float minX = float.MinValue;
        private float maxX = float.MaxValue;
        private float minY = float.MinValue;
        private float maxY = float.MaxValue;
        private OriginalInvisibleBarrierState upperBound, leftBound, rightBound;
        private OriginalKillboxEntity lowerBound;

        public OneWayCameraTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
                data.Position + offset, data.Width, data.Height,
                ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            left = data.Bool("left", false);
            right = data.Bool("right", false);
            up = data.Bool("up", false);
            down = data.Bool("down", false);
            blockPlayer = data.Bool("blockPlayer", false);
            flag = data.Attr("flag", "");
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            base.Update();
        }

        public override void Update()
        {
            base.Update();
            if (!string.IsNullOrEmpty(flag)) Collidable = ((OriginalLevelSceneRoot)Scene).Session.GetFlag(flag);
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            base.OnEnter(player);
            if (!blockPlayer) return;
            var level = (OriginalLevelSceneRoot)Scene;
            if (!left)
            {
                leftBound = new OriginalInvisibleBarrierState(level.EngineContext,
                    level.Camera.Position - Vector2.UnitX * 9f, 8f, 180f, level.SolidWorldFactory);
                level.SourceScene.QueueSourceEntity(leftBound);
            }

            if (!right)
            {
                rightBound = new OriginalInvisibleBarrierState(level.EngineContext,
                    level.Camera.Position + Vector2.UnitX * 320f, 8f, 180f, level.SolidWorldFactory);
                level.SourceScene.QueueSourceEntity(rightBound);
            }

            if (!up)
            {
                upperBound = new OriginalInvisibleBarrierState(level.EngineContext,
                    level.Camera.Position - Vector2.UnitY * 9f, 320f, 8f, level.SolidWorldFactory);
                level.SourceScene.QueueSourceEntity(upperBound);
            }

            if (!down)
            {
                lowerBound = new OriginalKillboxEntity(level.EngineContext, new EntityData { Width = 320 },
                    level.Camera.Position + Vector2.UnitY * 186f);
                level.SourceScene.QueueSourceEntity(lowerBound);
            }
        }

        public override void OnStay(RoomPlayerSession player)
        {
            base.OnStay(player);
            var level = (OriginalLevelSceneRoot)Scene;
            if (!left) minX = Math.Max(minX, level.Camera.X);
            if (!right) maxX = Math.Min(maxX, level.Camera.X);
            if (!up) minY = Math.Max(minY, level.Camera.Y);
            if (!down) maxY = Math.Min(maxY, level.Camera.Y);
            if (leftBound != null) leftBound.Position = level.Camera.Position - Vector2.UnitX * 9f;
            if (rightBound != null) rightBound.Position = level.Camera.Position + Vector2.UnitX * 320f;
            if (upperBound != null) upperBound.Position = level.Camera.Position - Vector2.UnitY * 9f;
            if (lowerBound != null) lowerBound.Position = level.Camera.Position + Vector2.UnitY * (180f + 6f);
        }

        public override void OnLeave(RoomPlayerSession player)
        {
            base.OnLeave(player);
            minX = float.MinValue;
            minY = float.MinValue;
            maxX = float.MaxValue;
            maxY = float.MaxValue;
            RemoveBounds();
        }

        public override void Removed(object scene)
        {
            base.Removed(scene);
            RemoveBounds();
        }

        private void RemoveBounds()
        {
            leftBound?.RemoveSelf();
            rightBound?.RemoveSelf();
            upperBound?.RemoveSelf();
            lowerBound?.RemoveSelf();
        }

        internal Vector2 ClampTarget(Vector2 target)
        {
            target.X = Clamp(target.X, minX, maxX);
            target.Y = Clamp(target.Y, minY, maxY);
            return target;
        }

        private static float Clamp(float value, float min, float max)
        {
            value = value > max ? max : value;
            value = value < min ? min : value;
            return value;
        }
    }
}
