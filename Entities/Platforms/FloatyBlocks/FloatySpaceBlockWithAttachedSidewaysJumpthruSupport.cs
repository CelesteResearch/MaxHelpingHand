using Celeste.Game.Gameplay.Platforms.FloatySpaceBlocks;
using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Collections;
using Celeste.Core.Components;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Randomness;
using Celeste.Core.Scheduling;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.FloatySpaceBlock;
using Celeste.Simulation.Gameplay.Platforms.StaticMovers;
using Celeste.Simulation.Gameplay.Interaction.Springs;
using Celeste.Simulation.Gameplay.Player.Collision.Surfaces;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Game.Gameplay.Interaction.Springs;
using Celeste.Game.Audio.Surfaces;
using Celeste.Game.Execution.Graphics;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Tiles;
using Celeste.Graphics.Content.Tiles.Autotiling;
using Celeste.Graphics.Animation;

namespace Celeste.Mods.MaxHelpingHand.Entities.Platforms.FloatyBlocks
{
    [Celeste.Everest.Modules.Entities.Metadata.CustomEntity(
        "MaxHelpingHand/FloatySpaceBlockWithAttachedSidewaysJumpthruSupport=Load")]
    [Celeste.Scene.Tracking.Attributes.SceneTracked]
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias(
        "Celeste.Mod.MaxHelpingHand.Entities.FloatySpaceBlockWithAttachedSidewaysJumpthruSupport")]
    public class FloatySpaceBlockWithAttachedSidewaysJumpthruSupport : SourceSolidEntityState
    {
        private readonly ISourceEngineContext engine;
        private TileGrid tiles;
        private List<AttachedSidewaysJumpThru> attachedSidewaysJumpthrus;

        private char tileType;

        private float yLerp;

        private float sinkTimer;

        private float sineWave;

        private float dashEase;

        private Vector2 dashDirection;

        private FloatySpaceBlockWithAttachedSidewaysJumpthruSupport master;

        private bool awake;

        private List<FloatySpaceBlockWithAttachedSidewaysJumpthruSupport> group;

        private List<SourceJumpThruEntityState> jumpthrus;

        private Dictionary<SourcePlatformEntityState, Vector2> moves;

        private IntPoint groupBoundsMin;

        private IntPoint groupBoundsMax;

        public bool HasGroup { get; private set; }

        public bool MasterOfGroup { get; private set; }

        public FloatySpaceBlockWithAttachedSidewaysJumpthruSupport(Vector2 position, float width, float height,
            char tileType,
            bool disableSpawnOffset,
            ISourceSolidWorld world)
            : base(position, width, height, true, world)
        {
            engine = world.EngineContext;
            this.tileType = tileType;
            base.Depth = -9000;
            OwnedComponents.Add(new LightOccludeState());
            SurfaceSoundIndex = ((IOriginalTileSurfaceEnvironment)engine).SurfaceIndices.TileToIndex[tileType];
            if (!disableSpawnOffset)
            {
                sineWave = SourceRandomMath.NextFloat(((ISourceRandomEnvironment)engine).Random, (float)Math.PI * 2f);
            }
            else
            {
                sineWave = 0f;
            }
        }

        public FloatySpaceBlockWithAttachedSidewaysJumpthruSupport(EntityData data, Vector2 offset,
            ISourceSolidWorldFactory worlds)
            : this(data.Position + offset, data.Width, data.Height,
                EntityAttributeReader.Char(data.Values, "tiletype", '3'),
                EntityAttributeReader.Bool(data.Values, "disableSpawnOffset"),
                worlds.Create(data.Position + offset, true))
        {
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            awake = true;
            if (!HasGroup)
            {
                MasterOfGroup = true;
                moves = new Dictionary<SourcePlatformEntityState, Vector2>();
                group = new List<FloatySpaceBlockWithAttachedSidewaysJumpthruSupport>();
                jumpthrus = new List<SourceJumpThruEntityState>();
                attachedSidewaysJumpthrus = new List<AttachedSidewaysJumpThru>();
                groupBoundsMin = new IntPoint((int)base.X, (int)base.Y);
                groupBoundsMax = new IntPoint((int)base.Right, (int)base.Bottom);
                AddToGroupAndFindChildren(this);
                _ = base.Scene;
                tiles = Celeste.Game.Presentation.Platforms.FloatySpaceBlocks.OriginalFloatyBlockGroupTiles.Generate(
                    group,
                    groupBoundsMin, groupBoundsMax, ref tileType, engine);
                tiles.Position = new Vector2((float)groupBoundsMin.X - base.X, (float)groupBoundsMin.Y - base.Y);
                OwnedComponents.Add(tiles);
            }

            TryToInitPosition();
        }

        public override void OnStaticMoverTrigger(SourceStaticMoverState sm)
        {
            FloatySpringReaction.Apply(sm, ref sinkTimer, ref dashEase, ref dashDirection);
        }

        private void TryToInitPosition()
        {
            if (MasterOfGroup)
            {
                foreach (FloatySpaceBlockWithAttachedSidewaysJumpthruSupport item in group)
                {
                    if (!item.awake)
                    {
                        return;
                    }
                }

                MoveToTarget();
            }
            else
            {
                master.TryToInitPosition();
            }
        }

        private void AddToGroupAndFindChildren(FloatySpaceBlockWithAttachedSidewaysJumpthruSupport from)
        {
            if (from.X < (float)groupBoundsMin.X)
            {
                groupBoundsMin.X = (int)from.X;
            }

            if (from.Y < (float)groupBoundsMin.Y)
            {
                groupBoundsMin.Y = (int)from.Y;
            }

            if (from.Right > (float)groupBoundsMax.X)
            {
                groupBoundsMax.X = (int)from.Right;
            }

            if (from.Bottom > (float)groupBoundsMax.Y)
            {
                groupBoundsMax.Y = (int)from.Bottom;
            }

            from.HasGroup = true;
            from.OnDashCollide = OnDash;
            group.Add(from);
            moves.Add(from, from.Position);
            if (from != this)
            {
                from.master = this;
            }

            foreach (SourceJumpThruEntityState item in SourceSceneCollision.AllRect<SourceJumpThruEntityState>(
                         ((ISourceScene)Scene).SourceScene.Entities, "Celeste.JumpThru",
                         new IntRect((int)from.X - 1, (int)from.Y, (int)from.Width + 2, (int)from.Height)))
            {
                if (!jumpthrus.Contains(item))
                {
                    AddJumpThru(item);
                }
            }

            foreach (SourceJumpThruEntityState item2 in SourceSceneCollision.AllRect<SourceJumpThruEntityState>(
                         ((ISourceScene)Scene).SourceScene.Entities, "Celeste.JumpThru",
                         new IntRect((int)from.X, (int)from.Y - 1, (int)from.Width, (int)from.Height + 2)))
            {
                if (!jumpthrus.Contains(item2))
                {
                    AddJumpThru(item2);
                }
            }

            foreach (AttachedSidewaysJumpThru item in SourceSceneCollision.AllRect<AttachedSidewaysJumpThru>(
                         ((ISourceScene)Scene).SourceScene.Entities,
                         ((ISourceScene)Scene).SourceScene.Catalog.Find(typeof(AttachedSidewaysJumpThru)).RegistryKey,
                         new IntRect((int)from.X - 1, (int)from.Y, (int)from.Width + 2, (int)from.Height)))
                if (!attachedSidewaysJumpthrus.Contains(item))
                    AddAttachedSidewaysJumpThru(item);
            foreach (AttachedSidewaysJumpThru item in SourceSceneCollision.AllRect<AttachedSidewaysJumpThru>(
                         ((ISourceScene)Scene).SourceScene.Entities,
                         ((ISourceScene)Scene).SourceScene.Catalog.Find(typeof(AttachedSidewaysJumpThru)).RegistryKey,
                         new IntRect((int)from.X, (int)from.Y - 1, (int)from.Width, (int)from.Height + 2)))
                if (!attachedSidewaysJumpthrus.Contains(item))
                    AddAttachedSidewaysJumpThru(item);
            foreach (FloatySpaceBlockWithAttachedSidewaysJumpthruSupport entity in ((ISourceScene)Scene).SourceScene
                     .Entities.GetTrackedEntities(
                         typeof(FloatySpaceBlockWithAttachedSidewaysJumpthruSupport)))
            {
                if (!entity.HasGroup && entity.tileType == tileType &&
                    (SourceSceneCollision.CheckRect(
                         new IntRect((int)from.X - 1, (int)from.Y, (int)from.Width + 2, (int)from.Height), entity) ||
                     SourceSceneCollision.CheckRect(
                         new IntRect((int)from.X, (int)from.Y - 1, (int)from.Width, (int)from.Height + 2), entity)))
                {
                    AddToGroupAndFindChildren(entity);
                }
            }
        }

        private void AddJumpThru(SourceJumpThruEntityState jp)
        {
            jp.OnDashCollide = OnDash;
            jumpthrus.Add(jp);
            moves.Add(jp, jp.Position);
            foreach (FloatySpaceBlockWithAttachedSidewaysJumpthruSupport entity in ((ISourceScene)Scene).SourceScene
                     .Entities.GetTrackedEntities(
                         typeof(FloatySpaceBlockWithAttachedSidewaysJumpthruSupport)))
            {
                if (!entity.HasGroup && entity.tileType == tileType &&
                    SourceSceneCollision.CheckRect(
                        new IntRect((int)jp.X - 1, (int)jp.Y, (int)jp.Width + 2, (int)jp.Height),
                        entity))
                {
                    AddToGroupAndFindChildren(entity);
                }
            }
        }

        public static FloatySpaceBlockWithAttachedSidewaysJumpthruSupport Load(
            Celeste.Game.Execution.Levels.OriginalLevelSceneRoot level,
            Celeste.Game.Content.Maps.Levels.OriginalLevelRoomData room, Vector2 offset,
            Celeste.Game.Content.Maps.Entities.OriginalLevelEntityData data)
            => new(data, offset, level.SolidWorldFactory);

        private void AddAttachedSidewaysJumpThru(AttachedSidewaysJumpThru jp)
        {
            jp.OnDashCollide = OnDash;
            attachedSidewaysJumpthrus.Add(jp);
            foreach (FloatySpaceBlockWithAttachedSidewaysJumpthruSupport entity in
                     ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(
                         typeof(FloatySpaceBlockWithAttachedSidewaysJumpthruSupport)))
                if (!entity.HasGroup && entity.tileType == tileType && SourceSceneCollision.CheckRect(
                        new IntRect((int)jp.X, (int)jp.Y - 1, (int)jp.Width, (int)jp.Height + 2), entity))
                    AddToGroupAndFindChildren(entity);
        }

        private DashCollisionResult OnDash(RoomPlayerSession player, Vector2 direction)
        {
            if (MasterOfGroup && dashEase <= 0.2f)
            {
                dashEase = 1f;
                dashDirection = direction;
            }

            return DashCollisionResult.NormalOverride;
        }

        public override void Update()
        {
            base.Update();
            if (MasterOfGroup)
            {
                bool flag = false;
                foreach (FloatySpaceBlockWithAttachedSidewaysJumpthruSupport item in group)
                {
                    if (item.HasPlayerRider())
                    {
                        flag = true;
                        break;
                    }
                }

                if (!flag)
                {
                    foreach (SourceJumpThruEntityState jumpthru in jumpthrus)
                    {
                        if (jumpthru.HasPlayerRider())
                        {
                            flag = true;
                            break;
                        }
                    }
                }

                if (!flag)
                    foreach (AttachedSidewaysJumpThru jp in attachedSidewaysJumpthrus)
                        if (Celeste.Mods.MaxHelpingHand.Runtime.Platforms.Movement.SourceSideWallPlatformMovement
                                .GetPlayerClimbing(jp, jp.Left) != null)
                        {
                            flag = true;
                            break;
                        }

                if (flag)
                {
                    sinkTimer = 0.3f;
                }
                else if (sinkTimer > 0f)
                {
                    sinkTimer -= engine.DeltaTime;
                }

                if (sinkTimer > 0f)
                {
                    yLerp = ScalarMath.Approach(yLerp, 1f, 1f * engine.DeltaTime,
                        ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                }
                else
                {
                    yLerp = ScalarMath.Approach(yLerp, 0f, 1f * engine.DeltaTime,
                        ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                }

                sineWave += engine.DeltaTime;
                dashEase = ScalarMath.Approach(dashEase, 0f, engine.DeltaTime * 1.5f,
                    ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                MoveToTarget();
            }

            LiftSpeed = Vector2.Zero;
        }

        private void MoveToTarget()
        {
            float num = ((ISourceFloatySpaceEnvironment)engine).FloatySpaceArithmetic.GetSine(sineWave);
            Vector2 vector = ScalarMath.YoYo(((ISourceEasingEnvironment)engine).EasingArithmetic.QuadIn(dashEase)) *
                             dashDirection * 8f;
            FloatyPlatformGroupMotion.Move(ref moves, num, vector, new FloatyTarget(this));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private float GetMoveTargetY(float origin, float sine, float dash)
            => ((ISourceFloatySpaceEnvironment)engine).FloatySpaceArithmetic.GetTargetY(origin,
                ((ISourceEasingEnvironment)engine).EasingArithmetic.SineInOut(yLerp), sine, dash);

        private readonly struct FloatyTarget : IFloatyPlatformTarget
        {
            private readonly FloatySpaceBlockWithAttachedSidewaysJumpthruSupport owner;
            public FloatyTarget(FloatySpaceBlockWithAttachedSidewaysJumpthruSupport owner) => this.owner = owner;
            public float GetTargetY(float origin, float sine, float dashY) => owner.GetMoveTargetY(origin, sine, dashY);
        }

        public override void OnShake(Vector2 amount)
        {
            if (!MasterOfGroup)
            {
                return;
            }

            base.OnShake(amount);
            tiles.Position += amount;
            foreach (SourceJumpThruEntityState jumpthru in jumpthrus)
            {
                foreach (SourceComponentState component in jumpthru.OwnedComponents)
                {
                    if (component is SpriteImage image)
                    {
                        image.Position += amount;
                    }
                }
            }

            foreach (AttachedSidewaysJumpThru jp in attachedSidewaysJumpthrus)
            foreach (SourceComponentState component in jp.OwnedComponents)
                if (component is SpriteImage image)
                    image.Position += amount;
        }
    }
}
