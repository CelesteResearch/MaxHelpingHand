using System.Numerics;
using Celeste.Core.Geometry;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Tracking.Attributes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.MaxHelpingHand.Components.Platforms;
using Celeste.Mods.MaxHelpingHand.Entities;
using Celeste.Physics.Collision;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Collision.Directional;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Mods.MaxHelpingHand.Runtime.Platforms.Movement;
using Celeste.Simulation.Gameplay.Player.Collision.Surfaces;

namespace Celeste.Mods.MaxHelpingHand.Entities.Platforms
{
    [CustomEntity("MaxHelpingHand/AttachedSidewaysJumpThru")]
    [SceneTracked]
    [EverestTrackedAs(typeof(SidewaysJumpThru))]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.AttachedSidewaysJumpThru")]
    public class AttachedSidewaysJumpThru : SidewaysJumpThru
    {
        private readonly SourceSolidEntityState playerInteractingSolid;
        public readonly new bool Left;
        public System.Func<RoomPlayerSession, Vector2, DashCollisionResult> OnDashCollide;
        private Vector2 shakeOffset;

        public static AttachedSidewaysJumpThru Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
            => new(level.EngineContext, data, offset, ((ISourcePlatformCollisionScene)level).SolidWorldFactory);

        public AttachedSidewaysJumpThru(ISourceEngineContext engine, EntityData data, Vector2 offset,
            ISourceSolidWorldFactory worlds)
            : base(engine, data.Position + offset, data.Height, !data.Bool("left"), data.Attr("texture", "default"),
                data.Float("animationDelay", 0f), data.Bool("allowClimbing", true), data.Bool("allowWallJumping", true),
                data.Bool("letSeekersThrough"), data.Int("surfaceIndex", -1), data.Bool("pushPlayer"),
                data.Bool("cornerCorrect"))
        {
            Left = data.Bool("left");
            playerInteractingSolid =
                new SourceSolidEntityState(Position, Width, Height, false, worlds.Create(Position, false))
                {
                    Collidable = false,
                    Visible = false
                };
            if (!Left) playerInteractingSolid.Position += Vector2.UnitX * 3f;
            var mover = new StaticMoverWithLiftSpeed
            {
                SolidChecker = solid =>
                    SourceEntityCollision.CheckRect(solid,
                        new IntRect((int)X, (int)Y - 1, (int)Width, (int)Height + 2)),
                OnMove = move => SourceSideWallPlatformMovement.Move(this, playerInteractingSolid, Left, move),
                OnShake = OnShake,
                OnSetLiftSpeed = lift => playerInteractingSolid.LiftSpeed = lift
            };
            if (data.Bool("toggleStaticMovers"))
            {
                mover.OnDisable = () =>
                {
                    Active = Visible = Collidable = false;
                    playerInteractingSolid.DisableStaticMovers();
                };
                mover.OnEnable = () =>
                {
                    Active = Visible = Collidable = true;
                    playerInteractingSolid.EnableStaticMovers();
                };
                mover.OnDestroy = () =>
                {
                    RemoveSelf();
                    playerInteractingSolid.DestroyStaticMovers();
                };
            }

            Add(mover);
        }

        private void OnShake(Vector2 amount)
        {
            shakeOffset += amount;
            playerInteractingSolid.ShakeStaticMovers(amount);
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            ((ISourceScene)scene).SourceScene.QueueSourceEntity(playerInteractingSolid);
        }

        public override ISourceEntityCollider GetMovementCollisionTarget(ISourceEntityCollider mover)
            => DirectionalCollisionTargetFactory.Create(((ISourcePlatformCollisionScene)Scene).SolidWorldFactory,
                mover is RoomPlayerSession player && player.DashAttacking ? OnDashCollide : null);

        public override void Render(ISourceRenderContext context)
        {
            Position += shakeOffset;
            base.Render(context);
            Position -= shakeOffset;
        }
    }
}
