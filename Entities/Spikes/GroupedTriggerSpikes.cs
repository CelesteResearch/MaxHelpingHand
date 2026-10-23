// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Core.Mathematics;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Platforms.Cassette;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Rendering;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Collision.Blockers;
using Celeste.Simulation.Gameplay.Hazards.Spikes;
using Celeste.Simulation.Gameplay.Hazards.TriggerSpikes;
using Celeste.Simulation.Gameplay.Hazards.TriggerSpikes.Components;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.StaticMovers;
using Celeste.Simulation.Gameplay.Player.Collision.Components;

namespace Celeste.Mods.MaxHelpingHand.Entities.Spikes
{
    /// <summary>
    /// TriggerSpikes that all come out when leaving the group.
    /// </summary>
    [CustomEntity(
        "MaxHelpingHand/GroupedTriggerSpikesUp = LoadUp",
        "MaxHelpingHand/GroupedTriggerSpikesDown = LoadDown",
        "MaxHelpingHand/GroupedTriggerSpikesLeft = LoadLeft",
        "MaxHelpingHand/GroupedTriggerSpikesRight = LoadRight"
    )]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.GroupedTriggerSpikes")]
    public class GroupedTriggerSpikes : SceneEntityState, ISourceEngineContextEntity
    {
        public static SceneEntityState LoadUp(OriginalLevelSceneRoot level, OriginalLevelRoomData levelData,
            Vector2 offset, OriginalLevelEntityData entityData)
        {
            if (EntityAttributeReader.Attr(entityData.Values, "type", "default") == "dust")
            {
                return new GroupedDustTriggerSpikes(entityData, offset, TriggerSpikesDirection.Up, level.EngineContext);
            }

            return new GroupedTriggerSpikes(entityData, offset, Directions.Up, level.EngineContext);
        }

        public static SceneEntityState LoadDown(OriginalLevelSceneRoot level, OriginalLevelRoomData levelData,
            Vector2 offset, OriginalLevelEntityData entityData)
        {
            if (EntityAttributeReader.Attr(entityData.Values, "type", "default") == "dust")
            {
                return new GroupedDustTriggerSpikes(entityData, offset, TriggerSpikesDirection.Down,
                    level.EngineContext);
            }

            return new GroupedTriggerSpikes(entityData, offset, Directions.Down, level.EngineContext);
        }

        public static SceneEntityState LoadLeft(OriginalLevelSceneRoot level, OriginalLevelRoomData levelData,
            Vector2 offset, OriginalLevelEntityData entityData)
        {
            if (EntityAttributeReader.Attr(entityData.Values, "type", "default") == "dust")
            {
                return new GroupedDustTriggerSpikes(entityData, offset, TriggerSpikesDirection.Left,
                    level.EngineContext);
            }

            return new GroupedTriggerSpikes(entityData, offset, Directions.Left, level.EngineContext);
        }

        public static SceneEntityState LoadRight(OriginalLevelSceneRoot level, OriginalLevelRoomData levelData,
            Vector2 offset, OriginalLevelEntityData entityData)
        {
            if (EntityAttributeReader.Attr(entityData.Values, "type", "default") == "dust")
            {
                return new GroupedDustTriggerSpikes(entityData, offset, TriggerSpikesDirection.Right,
                    level.EngineContext);
            }

            return new GroupedTriggerSpikes(entityData, offset, Directions.Right, level.EngineContext);
        }

        private const float DelayTime = 0.4f;
        private readonly ISourceEngineContext engine;
        public ISourceEngineContext EngineContext => engine;

        public bool Triggered = false;
        public float DelayTimer;
        public float Lerp;

        private int size;
        private Directions direction;
        private string overrideType;
        private bool triggerIfSameDirection;
        private bool killIfSameDirection;

        private Vector2 outwards;

        private Vector2 shakeOffset;

        private string spikeType;

        private Vector2[] spikePositions;
        private List<AtlasEntry> spikeTextures;

        // "attached to cassette block" stuff
        private PackedColor enabledColor = PackedColor.White;
        private PackedColor disabledColor = PackedColor.White;
        private bool visibleWhenDisabled = false;

        private bool blockingLedge = false;

        public GroupedTriggerSpikes(EntityData data, Vector2 offset, Directions dir, ISourceEngineContext engine)
            : this(data.Position + offset, GetSize(data, dir), dir,
                EntityAttributeReader.Attr(data.Values, "type", "default"),
                EntityAttributeReader.Bool(data.Values, "behindMoveBlocks", false),
                EntityAttributeReader.Bool(data.Values, "triggerIfSameDirection", false),
                EntityAttributeReader.Bool(data.Values, "killIfSameDirection",
                    EntityAttributeReader.Bool(data.Values, "triggerIfSameDirection", false)), engine)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public GroupedTriggerSpikes(Vector2 position, int size, Directions direction, string overrideType,
            bool behindMoveBlocks, ISourceEngineContext engine)
            : this(position, size, direction, overrideType, behindMoveBlocks, triggerIfSameDirection: false,
                killIfSameDirection: false, engine)
        {
        }

        public GroupedTriggerSpikes(Vector2 position, int size, Directions direction, string overrideType,
            bool behindMoveBlocks, bool triggerIfSameDirection, bool killIfSameDirection, ISourceEngineContext engine)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, position)
        {
            this.engine = engine;
            this.size = size;
            this.direction = direction;
            this.overrideType = overrideType;
            this.triggerIfSameDirection = triggerIfSameDirection;
            this.killIfSameDirection = killIfSameDirection;

            switch (direction)
            {
                case Directions.Up:
                    outwards = new Vector2(0f, -1f);
                    Collider = new HitboxColliderGeometry(size, 3f, 0f, -3f);
                    Add(new SafeGroundBlockerComponentState());
                    Add(new LedgeBlockerState(p => UpSafeBlockCheck((RoomPlayerSession)p)));
                    break;

                case Directions.Down:
                    outwards = new Vector2(0f, 1f);
                    Collider = new HitboxColliderGeometry(size, 3f, 0f, 0f);
                    break;

                case Directions.Left:
                    outwards = new Vector2(-1f, 0f);
                    Collider = new HitboxColliderGeometry(3f, size, -3f, 0f);

                    Add(new SafeGroundBlockerComponentState());
                    Add(new LedgeBlockerState(p => SideSafeBlockCheck((RoomPlayerSession)p)));
                    break;

                case Directions.Right:
                    outwards = new Vector2(1f, 0f);
                    Collider = new HitboxColliderGeometry(3f, size, 0f, 0f);

                    Add(new SafeGroundBlockerComponentState());
                    Add(new LedgeBlockerState(p => SideSafeBlockCheck((RoomPlayerSession)p)));
                    break;
            }

            Add(new PlayerColliderComponentState(OnCollide));

            Add(new SourceStaticMoverState
            {
                OnShake = OnShake,
                SolidChecker = CheckAttachToSolid,
                JumpThruChecker = IsRiding,
                OnEnable = OnEnable,
                OnDisable = OnDisable
            });

            if (behindMoveBlocks)
            {
                Depth = 0; // move blocks have Depth = -1.
            }
            else
            {
                Depth = -50;
            }
        }

        public override void Added(object scene)
        {
            base.Added(scene);

            spikeType = ((ISourceSpikeScene)scene).SpikeType;
            if (!string.IsNullOrEmpty(overrideType) && overrideType != "default")
            {
                spikeType = overrideType;
            }

            string dir = direction.ToString().ToLower();

            if (spikeType == "tentacles")
            {
                throw new NotSupportedException("Trigger tentacles currently not supported");
            }

            spikePositions = new Vector2[size / 8];
            spikeTextures =
                ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas.GetAtlasSubtextures("danger/spikes/" +
                    spikeType + "_" + dir);
            for (int i = 0; i < spikePositions.Length; i++)
            {
                switch (direction)
                {
                    case Directions.Up:
                        spikePositions[i] = Vector2.UnitX * (i + 0.5f) * 8f + Vector2.UnitY;
                        break;

                    case Directions.Down:
                        spikePositions[i] = Vector2.UnitX * (i + 0.5f) * 8f - Vector2.UnitY;
                        break;

                    case Directions.Left:
                        spikePositions[i] = Vector2.UnitY * (i + 0.5f) * 8f + Vector2.UnitX;
                        break;

                    case Directions.Right:
                        spikePositions[i] = Vector2.UnitY * (i + 0.5f) * 8f - Vector2.UnitX;
                        break;
                }
            }
        }

        private bool CheckAttachToSolid(SourceSolidEntityState solid)
        {
            bool collides = IsRiding(solid);

            // if we happen to be attached to a cassette block, take its color.
            if (collides && solid is CassetteBlockState cassetteBlock)
            {
                enabledColor = cassetteBlock.Color;

                // same formula as cassette blocks do.
                PackedColor color = new PackedColor(102, 125, 165);
                disabledColor = new PackedColor(
                    color.R / 255f * (enabledColor.R / 255f),
                    color.G / 255f * (enabledColor.G / 255f),
                    color.B / 255f * (enabledColor.B / 255f),
                    1f);

                visibleWhenDisabled = true;
            }

            return collides;
        }

        private void OnEnable()
        {
            Collidable = Visible = true;
        }

        private void OnDisable()
        {
            Collidable = false;
            Visible = visibleWhenDisabled;
        }

        private void OnShake(Vector2 amount)
        {
            shakeOffset += amount;
        }

        private bool UpSafeBlockCheck(RoomPlayerSession player)
        {
            int dir = 8 * (int)player.Data.Facing;
            int left = (int)((player.Left + dir - Left) / 4f);
            int right = (int)((player.Right + dir - Left) / 4f);

            if (right < 0 || left >= spikePositions.Length)
            {
                return false;
            }

            return Lerp >= 1f;
        }

        private bool SideSafeBlockCheck(RoomPlayerSession player)
        {
            int top = (int)((player.Top - Top) / 4f);
            int bottom = (int)((player.Bottom - Top) / 4f);

            if (bottom < 0 || top >= spikePositions.Length)
                return false;

            return Lerp >= 1f;
        }

        private void OnCollide(RoomPlayerSession player)
        {
            GetPlayerCollideIndex(player, out int minIndex, out int maxIndex);
            if (minIndex < 0 || minIndex >= spikePositions.Length)
            {
                return;
            }

            if (!Triggered)
            {
                ((ISourceSpikeScene)Scene).PlaySpikeSound("event:/game/03_resort/fluff_tendril_touch",
                    Position + spikePositions[minIndex]);
                Triggered = true;
                DelayTimer = DelayTime;
            }
            else if (Lerp >= 1f)
            {
                player.Die(outwards);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void GetPlayerCollideIndex(RoomPlayerSession player, out int minIndex, out int maxIndex)
            => TriggerSpikeCollision.GetPlayerCollideIndices(player, this, (TriggerSpikesDirection)direction,
                8f, out minIndex, out maxIndex, EvenIfSameDirection());

        private bool EvenIfSameDirection()
        {
            if (Triggered)
            {
                return killIfSameDirection;
            }
            else
            {
                return triggerIfSameDirection;
            }
        }

        private static int GetSize(EntityData data, Directions dir)
        {
            return
                dir > Directions.Down ? data.Height : data.Width;
        }

        public override void Update()
        {
            base.Update();

            if (Triggered)
            {
                if (DelayTimer > 0f)
                {
                    DelayTimer -= engine.DeltaTime;
                    if (DelayTimer <= 0f)
                    {
                        if (FirstPlayer() != null)
                        {
                            DelayTimer = 0.05f;
                        }
                        else
                        {
                            ((ISourceSpikeScene)Scene).PlaySpikeSound("event:/game/03_resort/fluff_tendril_emerge",
                                Position + spikePositions[spikePositions.Length / 2]);
                        }
                    }
                }
                else
                {
                    Lerp = ScalarMath.Approach(Lerp, 1f, 8f * engine.DeltaTime);
                }
            }
            else
            {
                Lerp = ScalarMath.Approach(Lerp, 0f, 4f * engine.DeltaTime);
                if (Lerp <= 0f)
                {
                    Triggered = false;
                }
            }

            // "climb hopping" should be blocked if trigger spikes are going to kill Madeline (Lerp >= 1).
            // this is done by adding a LedgeBlocker component.
            if (blockingLedge != (Lerp >= 1f))
            {
                blockingLedge = !blockingLedge;

                // add or remove the ledge blocker depending on the need.
                if (blockingLedge)
                {
                    Add(new LedgeBlockerState());
                }
                else
                {
                    foreach (SourceComponentState component in this)
                    {
                        if (component is LedgeBlockerState)
                        {
                            Remove(component);
                            break;
                        }
                    }
                }
            }
        }

        public override void Render(ISourceRenderContext context)
        {
            base.Render(context);

            Vector2 justify = Vector2.One * 0.5f;
            switch (direction)
            {
                case Directions.Up:
                    justify = new Vector2(0.5f, 1f);
                    break;
                case Directions.Down:
                    justify = new Vector2(0.5f, 0f);
                    break;
                case Directions.Left:
                    justify = new Vector2(1f, 0.5f);
                    break;
                case Directions.Right:
                    justify = new Vector2(0f, 0.5f);
                    break;
            }

            for (int i = 0; i < spikePositions.Length; i++)
            {
                AtlasEntry tex = spikeTextures[0];
                Vector2 pos = Position + shakeOffset + spikePositions[i] + outwards * (-4f + Lerp * 4f);
                SourceAtlasDrawing.DrawJustified(context, this, tex, pos, justify,
                    Collidable ? enabledColor : disabledColor, Vector2.One, 0f);
            }
        }

        private bool IsRiding(SourceSolidEntityState solid)
        {
            switch (direction)
            {
                case Directions.Up:
                    return CheckOutside(solid, Position + Vector2.UnitY);
                case Directions.Down:
                    return CheckOutside(solid, Position - Vector2.UnitY);
                case Directions.Left:
                    return CheckOutside(solid, Position + Vector2.UnitX);
                case Directions.Right:
                    return CheckOutside(solid, Position - Vector2.UnitX);
                default:
                    return false;
            }
        }

        private bool IsRiding(ISourceJumpThruEntity jumpThru)
        {
            return direction == Directions.Up &&
                   SourceEntityCollision.CheckAt(this, jumpThru, Position + Vector2.UnitY);
        }

        public enum Directions
        {
            Up,
            Down,
            Left,
            Right
        }

        private bool CheckOutside(ISourceEntityCollider other, Vector2 at)
            => !SourceEntityCollision.Check(this, other) && SourceEntityCollision.CheckAt(this, other, at);

        private RoomPlayerSession FirstPlayer()
        {
            foreach (object candidate in ((ISourceSpikeScene)Scene).SourceScene.Entities.GetTrackedEntities(
                         typeof(RoomPlayerSession).FullName))
                if (SourceEntityCollision.Check(this, (ISourceEntityCollider)candidate))
                    return (RoomPlayerSession)candidate;
            return null;
        }
    }
}
