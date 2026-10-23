using System;
using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Randomness;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Enemies.Seekers;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Mods.MaxHelpingHand.Modules;
using Celeste.Physics.Collision;
using Celeste.Mods.MaxHelpingHand.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Collision.Directional;
using Celeste.Simulation.Gameplay.Platforms.Collision;

namespace Celeste.Mods.MaxHelpingHand.Entities
{
    [CustomEntity("MaxHelpingHand/SidewaysJumpThru = CreateSidewaysJumpThru",
        "MaxHelpingHand/OneWayInvisibleBarrierHorizontal = CreateOneWayInvisibleBarrierHorizontal")]
    [SceneTracked]
    [global::Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.SidewaysJumpThru")]
    public class SidewaysJumpThru : SceneEntityState, IDirectionalOneWayCollider, IWallSurfaceSoundCollider
    {
        private readonly ISourceEngineContext engine;
        private readonly MaxHelpingHandModule module;
        private readonly int lines;
        private readonly string overrideTexture;
        private readonly float animationDelay;
        private readonly bool letSeekersThrough;
        private readonly bool pushPlayer;
        private readonly bool cornerCorrect;
        private IDisposable collisionRegistration;

        public bool AllowLeftToRight;
        public bool AllowPositiveHorizontalMovement => AllowLeftToRight;
        public bool AllowClimbing { get; }
        public bool AllowWallJumping { get; }
        public int SurfaceIndex { get; }
        public int WallSurfaceSoundIndex => SurfaceIndex;

        public static SidewaysJumpThru CreateSidewaysJumpThru(OriginalLevelSceneRoot level,
            OriginalLevelRoomData room, Vector2 offset, OriginalLevelEntityData data)
            => new(level.EngineContext, data.Position + offset, data.Height, !data.Bool("left"),
                data.Attr("texture", "default"), data.Float("animationDelay", 0f), data.Bool("allowClimbing", true),
                data.Bool("allowWallJumping", true), data.Bool("letSeekersThrough"), data.Int("surfaceIndex", -1),
                data.Bool("pushPlayer"), data.Bool("cornerCorrect"));

        public static SidewaysJumpThru CreateOneWayInvisibleBarrierHorizontal(OriginalLevelSceneRoot level,
            OriginalLevelRoomData room, Vector2 offset, OriginalLevelEntityData data)
        {
            data.Values["allowClimbing"] = false;
            data.Values["allowWallJumping"] = false;
            data.Values["texture"] = "MaxHelpingHand/invisible";
            return CreateSidewaysJumpThru(level, room, offset, data);
        }

        public SidewaysJumpThru(ISourceEngineContext engine, Vector2 position, int height, bool allowLeftToRight,
            string overrideTexture, float animationDelay, bool allowClimbing, bool allowWallJumping,
            bool letSeekersThrough, int surfaceIndex, bool pushPlayer, bool cornerCorrect)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, position)
        {
            this.engine = engine;
            module = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<MaxHelpingHandModule>().Single();
            lines = height / 8;
            AllowLeftToRight = allowLeftToRight;
            this.overrideTexture = overrideTexture;
            this.animationDelay = animationDelay;
            AllowClimbing = allowClimbing;
            AllowWallJumping = allowWallJumping;
            this.letSeekersThrough = letSeekersThrough;
            SurfaceIndex = surfaceIndex;
            this.pushPlayer = pushPlayer;
            this.cornerCorrect = cornerCorrect;
            Depth = -60;
            Collider = new HitboxColliderGeometry(5, height, allowLeftToRight ? 3 : 0, 0);
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            collisionRegistration = module.RegisterDirectionalCollider(
                ((ISourcePlatformCollisionScene)scene).PlatformWorld, this);
        }

        public override void Removed(object scene)
        {
            ReleaseCollisionRegistration();
            base.Removed(scene);
        }

        public override void SceneBegin(object scene)
        {
            base.SceneBegin(scene);
            if (collisionRegistration == null)
                collisionRegistration = module.RegisterDirectionalCollider(
                    ((ISourcePlatformCollisionScene)scene).PlatformWorld, this);
        }

        public override void SceneEnd(object scene)
        {
            ReleaseCollisionRegistration();
            base.SceneEnd(scene);
        }

        private void ReleaseCollisionRegistration()
        {
            collisionRegistration?.Dispose();
            collisionRegistration = null;
        }

        public bool AllowsMovement(ISourceEntityCollider mover)
            => !(mover is OriginalSeekerState) || !letSeekersThrough;

        public bool AllowsEntityContact(ISourceEntityCollider mover)
            => mover is RoomPlayerSession || mover is OriginalSeekerState;

        public virtual ISourceEntityCollider GetMovementCollisionTarget(ISourceEntityCollider mover)
            => DirectionalCollisionTargetFactory.Create(((ISourcePlatformCollisionScene)Scene).SolidWorldFactory);

        public override void Awake(object scene)
        {
            var atlas = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas;
            if (animationDelay > 0f)
            {
                for (int i = 0; i < lines; i++)
                {
                    var sprite = new Sprite(engine, atlas, "objects/jumpthru/" + overrideTexture);
                    sprite.AddLoop("idle", "", animationDelay);
                    Place(sprite, i);
                    sprite.Play("idle");
                    Add(sprite);
                }

                return;
            }

            string jumpthru = ((OriginalLevelSceneRoot)scene).AreaData.Jumpthru;
            if (!string.IsNullOrEmpty(overrideTexture) && overrideTexture != "default") jumpthru = overrideTexture;
            var texture = atlas["objects/jumpthru/" + jumpthru];
            int count = texture.FrameWidth / 8;
            var random = ((ISourceRandomEnvironment)engine).Random;
            for (int i = 0; i < lines; i++)
            {
                int x;
                int y;
                if (i == 0)
                {
                    x = 0;
                    y = SolidAt(Position - Vector2.UnitY) ? 0 : 1;
                }
                else if (i == lines - 1)
                {
                    x = count - 1;
                    y = SolidAt(Position + Vector2.UnitY) ? 0 : 1;
                }
                else
                {
                    x = 1 + random.Next(count - 2);
                    y = RandomSelection.Choose(random, 0, 1);
                }

                var image = new SpriteImage(texture.GetSubtexture(x * 8, y * 8, 8, 8));
                Place(image, i);
                Add(image);
            }
        }

        private void Place(SourceGraphicsComponentState graphic, int line)
        {
            graphic.Y = line * 8;
            graphic.Rotation = (float)(Math.PI / 2);
            if (AllowLeftToRight) graphic.X = 8;
            else graphic.Scale.Y = -1;
        }

        private bool SolidAt(Vector2 at)
            => SourceTrackedEntityCollision.CheckAt(((ISourceScene)Scene).SourceScene.Entities,
                "Celeste.Solid", this, at);

        public override void Update()
        {
            base.Update();
            if (!pushPlayer && !cornerCorrect) return;
            RoomPlayerSession player = null;
            foreach (RoomPlayerSession candidate in ((ISourceScene)Scene).SourceScene.Entities
                     .GetTrackedEntities(typeof(RoomPlayerSession)))
                if (SourceEntityCollision.Check(this, candidate))
                {
                    player = candidate;
                    break;
                }

            if (player == null) return;

            if (pushPlayer && !player.Data.OnGround &&
                (player.Machine.State != 1 || player.Data.LastClimbMove == -1) &&
                !player.World.Contacts.JumpThruBoostBlocked())
            {
                if (AllowLeftToRight && player.Speed.X >= 0f) player.MoveH(40f * engine.DeltaTime);
                else if (!AllowLeftToRight && player.Speed.X <= 0f) player.MoveH(-40f * engine.DeltaTime);
            }

            if (cornerCorrect && (player.Machine.State == 2 || player.Machine.State == 5) &&
                Math.Abs(player.Data.Direction.X) < 0.1f)
            {
                float right = Collider.AbsoluteRight;
                float left = Collider.AbsoluteLeft;
                float playerLeft = player.Collider.AbsoluteLeft;
                float playerRight = player.Collider.AbsoluteRight;
                if (AllowLeftToRight && right - playerLeft <= 6f) player.MoveHExact((int)(right - playerLeft));
                else if (!AllowLeftToRight && playerRight - left <= 6f) player.MoveHExact((int)(left - playerRight));
            }
        }
    }
}
