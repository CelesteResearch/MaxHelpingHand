// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Entities.Platforms.Wood;
using Celeste.Game.Presentation.Platforms.Wood;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.StaticMovers;
using Celeste.Simulation.Presentation.Lighting;

namespace Celeste.Mods.MaxHelpingHand.Entities.Platforms
{
    [CustomEntity("MaxHelpingHand/MultiNodeMovingPlatform=Load")]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.MultiNodeMovingPlatform")]
    public partial class MultiNodeMovingPlatform : SourceJumpThruEntityState
    {
        private enum Mode
        {
            BackAndForth,
            BackAndForthNoPause,
            Loop,
            LoopNoPause,
            TeleportBack
        }

        private readonly ISourceEngineContext engine;
        private readonly ISourceSolidWorldFactory worlds;
        private readonly ISourceScalarArithmetic scalar;
        private readonly ISourceVectorArithmetic vector;
        private readonly Func<float, float> sineInOut;
        private Vector2[] nodes;
        private float moveTime, pauseTime;
        private string overrideTexture;
        private Mode mode;
        private bool easing;
        private int amount;
        private float startingOffset;
        private string flag;
        private bool moveLater, emitSound;
        internal readonly bool giveHorizontalBoost;
        private bool drawTracks, accurateTiming;
        private AtlasEntry[] textures;
        private float[] nodePercentages;
        private string lastSfx;
        private SoundSourceComponentState sfx;
        internal int sinkingDir = 1;
        private float pauseTimer;
        private int prevNodeIndex, nextNodeIndex = 1;
        private float percent;
        private float? percentRemainder;
        private int direction = 1;
        private bool teleporting, movingHorizontally;
        private float addY, sinkTimer;
        private readonly Dictionary<string, object> entityProperties;
        private readonly Vector2 entityPosition;
        private readonly int entityWidth;
        private readonly Vector2[] entityNodes;
        private readonly Vector2 entityOffset;
        private readonly Action<MultiNodeMovingPlatform> callbackOnAdded;
        private Vector2? forcedTrackOffset;
        private Vector2 previousPosition;
        private bool startingOffsetIsNotZero;

        public static MultiNodeMovingPlatform Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
            => new(data, offset, ((ISourcePlatformCollisionScene)level).SolidWorldFactory);

        public MultiNodeMovingPlatform(EntityData data, Vector2 offset, ISourceSolidWorldFactory worlds,
            Action<MultiNodeMovingPlatform> callbackOnAdded = null)
            : base(data.Position + offset, data.Width, false, worlds.CreateJumpThru(data.Position + offset, false))
        {
            this.worlds = worlds;
            engine = SourceWorld.EngineContext;
            scalar = ((ISourceScalarEnvironment)engine).ScalarArithmetic;
            vector = ((ISourceVectorEnvironment)engine).VectorArithmetic;
            sineInOut = ((ISourceEasingEnvironment)engine).EasingArithmetic.SineInOut;
            this.callbackOnAdded = callbackOnAdded;
            moveTime = data.Float("moveTime", 2f);
            pauseTime = data.Float("pauseTime", 0f);
            overrideTexture = data.Attr("texture", "default");
            mode = data.Enum("mode", Mode.Loop);
            easing = data.Bool("easing", true);
            amount = data.Int("amount", 1);
            flag = data.Attr("flag", "");
            moveLater = data.Bool("moveLater");
            emitSound = data.Bool("emitSound", true);
            giveHorizontalBoost = data.Bool("giveHorizontalBoost");
            drawTracks = data.Bool("drawTracks", true);
            accurateTiming = data.Bool("accurateTiming");
            entityProperties = data.Values;
            entityPosition = data.Position;
            entityWidth = data.Width;
            entityNodes = data.Nodes;
            entityOffset = offset;
            previousPosition = Position;
            nodes = new Vector2[data.Nodes.Length + 1];
            nodes[0] = data.Position + offset;
            for (int i = 0; i < data.Nodes.Length; i++) nodes[i + 1] = data.Nodes[i] + offset;
            Add(sfx = ((ISourceSoundSourceEnvironment)engine).SoundSources.Create());
            SurfaceSoundIndex = 5;
            lastSfx = Math.Sign(nodes[0].X - nodes[1].X) > 0 || Math.Sign(nodes[0].Y - nodes[1].Y) > 0
                ? OriginalMovingPlatformState.SfxLeft
                : OriginalMovingPlatformState.SfxRight;
            Add(new LightOccludeState(0.2f));
            InitializePathPercentages();
            InitializeOffset(data.Float("offset", 0f));
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            if (string.IsNullOrEmpty(overrideTexture))
                overrideTexture = ((OriginalLevelSceneRoot)scene).AreaData.WoodPlatform;
            var texture =
                ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas[
                    "objects/woodPlatform/" + overrideTexture];
            textures = new AtlasEntry[texture.FrameWidth / 8];
            for (int i = 0; i < textures.Length; i++) textures[i] = texture.GetSubtexture(i * 8, 0, 8, 8);
            var scheduler = ((ISourceScene)scene).SourceScene;
            if (drawTracks && (Visible || forcedTrackOffset.HasValue))
            {
                Vector2 offset = forcedTrackOffset ?? vector.Divide(new Vector2(Width, Height + 4f), 2f);
                scheduler.QueueSourceEntity(
                    new OriginalMovingPlatformLineState(engine, nodes[0] + offset, nodes[1] + offset));
                if (nodes.Length > 2)
                {
                    for (int i = 1; i < nodes.Length - 1; i++)
                        scheduler.QueueSourceEntity(
                            new OriginalMovingPlatformLineState(engine, nodes[i] + offset, nodes[i + 1] + offset));
                    if (mode is Mode.Loop or Mode.LoopNoPause)
                        scheduler.QueueSourceEntity(new OriginalMovingPlatformLineState(engine,
                            nodes[nodes.Length - 1] + offset, nodes[0] + offset));
                }
            }

            for (int i = 1; i < amount; i++)
            {
                float offset = startingOffset;
                offset += 1f / amount * i;
                if (offset >= 1f) offset -= 1f;
                var values = new Dictionary<string, object>(entityProperties) { ["offset"] = offset, ["amount"] = 1 };
                var data = new EntityData
                    { Position = entityPosition, Nodes = entityNodes, Width = entityWidth, Values = values };
                scheduler.QueueSourceEntity(new MultiNodeMovingPlatform(data, entityOffset, worlds, callbackOnAdded)
                    { Visible = Visible });
            }

            callbackOnAdded?.Invoke(this);
            if (startingOffsetIsNotZero && !moveLater) updatePosition();
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            if (startingOffsetIsNotZero && moveLater) updatePosition();
        }

        public override void Render(ISourceRenderContext context) =>
            WoodPlatformDrawing.Render(context, this, textures);

        public override void OnStaticMoverTrigger(SourceStaticMoverState mover) => sinkTimer = .4f;

        public override void MoveHExact(int move)
        {
            if (!giveHorizontalBoost || !movingHorizontally) base.MoveHExact(move);
            else MoveHExactWithLiftSpeed(move);
        }

        internal void AnimateObject(SourceStaticMoverState mover, Vector2? forcedTrackOffset = null)
        {
            staticMovers.Add(mover);
            Visible = false;
            Collider.Width = 8f;
            Collider.Position -= new Vector2(4f, 2f);
            this.forcedTrackOffset = forcedTrackOffset;
        }
    }
}
