using System;
using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Randomness;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Everest.Tracking.Attributes;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Mods.MaxHelpingHand.Modules;
using Celeste.Mods.MaxHelpingHand.Interop;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Vertical;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.StaticMovers;

namespace Celeste.Mods.MaxHelpingHand.Entities
{
    [CustomEntity("MaxHelpingHand/UpsideDownJumpThru = Create")]
    [SceneTracked]
    [EverestTrackedAs(typeof(SourceJumpThruEntityState))]
    [global::Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.UpsideDownJumpThru")]
    public class UpsideDownJumpThru : SourceJumpThruEntityState, IVerticalOneWayCollider
    {
        private readonly ISourceEngineContext engine;
        private readonly MaxHelpingHandModule module;
        private readonly GravityHelperImports gravityHelper;

        private readonly int columns;

        // Derived moving platforms can replace the texture, matching the upstream extension point.
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        protected string overrideTexture;
        private readonly float animationDelay;
        private readonly bool pushPlayer;
        private readonly bool squishPlayer;
        private IDisposable collisionRegistration;
        private Vector2 shakeOffset;

        public bool AllowPositiveVerticalMovement => true;

        public static UpsideDownJumpThru Create(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(level.EngineContext,
            ((ISourcePlatformCollisionScene)level).SolidWorldFactory, data, offset);

        public UpsideDownJumpThru(ISourceEngineContext engine, ISourceSolidWorldFactory worlds,
            OriginalLevelEntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, true, worlds.CreateJumpThru(data.Position + offset, true))
        {
            this.engine = engine;
            gravityHelper = GravityHelperImports.For(engine);
            module = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<MaxHelpingHandModule>().Single();
            columns = data.Width / 8;
            Depth = -60;
            overrideTexture = data.Attr("texture", "default");
            animationDelay = data.Float("animationDelay", 0f);
            pushPlayer = data.Bool("pushPlayer");
            bool attached = data.Bool("attached");
            squishPlayer = data.Bool("squishPlayer", attached);
            if (attached)
                Add(new SourceStaticMoverState
                {
                    SolidChecker = solid => SourceEntityCollision.CheckAt(this, solid, Position - Vector2.UnitX) ||
                                            SourceEntityCollision.CheckAt(this, solid, Position + Vector2.UnitX),
                    OnMove = amount =>
                    {
                        MoveH(amount.X);
                        MoveV(amount.Y);
                    },
                    OnShake = amount => shakeOffset += amount
                });
            Collider.Top += 3;
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            Register(scene);
        }

        public override void SceneBegin(object scene)
        {
            base.SceneBegin(scene);
            if (collisionRegistration == null) Register(scene);
        }

        private void Register(object scene) => collisionRegistration = module.RegisterVerticalCollider(
            ((ISourcePlatformCollisionScene)scene).PlatformWorld, this);

        public override void Removed(object scene)
        {
            Release();
            base.Removed(scene);
        }

        public override void SceneEnd(object scene)
        {
            Release();
            base.SceneEnd(scene);
        }

        private void Release()
        {
            collisionRegistration?.Dispose();
            collisionRegistration = null;
        }

        public override void Awake(object scene)
        {
            var atlas = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas;
            if (animationDelay > 0f)
            {
                for (int column = 0; column < columns; column++)
                {
                    var sprite = new Sprite(engine, atlas, "objects/jumpthru/" + overrideTexture);
                    sprite.AddLoop("idle", "", animationDelay);
                    Place(sprite, column);
                    sprite.Play("idle");
                    Add(sprite);
                }

                return;
            }

            string jumpthru = ((OriginalLevelSceneRoot)scene).AreaData.Jumpthru;
            if (!string.IsNullOrEmpty(overrideTexture) && overrideTexture != "default") jumpthru = overrideTexture;
            var texture = atlas["objects/jumpthru/" + jumpthru];
            int width = texture.FrameWidth / 8;
            var random = ((ISourceRandomEnvironment)engine).Random;
            for (int column = 0; column < columns; column++)
            {
                int x;
                int y;
                if (column == 0)
                {
                    x = 0;
                    y = SolidAt(Position - Vector2.UnitX) ? 0 : 1;
                }
                else if (column == columns - 1)
                {
                    x = width - 1;
                    y = SolidAt(Position + Vector2.UnitX) ? 0 : 1;
                }
                else
                {
                    x = 1 + random.Next(width - 2);
                    y = RandomSelection.Choose(random, 0, 1);
                }

                var image = new SpriteImage(texture.GetSubtexture(x * 8, y * 8, 8, 8));
                Place(image, column);
                Add(image);
            }
        }

        private static void Place(SourceGraphicsComponentState graphic, int column)
        {
            graphic.X = column * 8;
            graphic.Y = 8;
            graphic.Scale.Y = -1;
        }

        private bool SolidAt(Vector2 at) => SourceTrackedEntityCollision.CheckAt(
            ((ISourceScene)Scene).SourceScene.Entities, "Celeste.Solid", this, at);

        private RoomPlayerSession PlayerAt(Vector2 at)
        {
            foreach (RoomPlayerSession player in ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(
                         typeof(RoomPlayerSession)))
                if (SourceEntityCollision.CheckAt(this, player, at))
                    return player;
            return null;
        }

        public override void MoveVExact(int move)
        {
            bool playerInverted = gravityHelper.IsPlayerInverted();
            RoomPlayerSession player;
            while (!playerInverted && move > 0 && PlayerAt(Position) == null &&
                   (player = PlayerAt(Position + Vector2.UnitY * move)) != null)
                if (player.MoveVExact(1))
                {
                    if (squishPlayer) player.Die(Vector2.Zero);
                    break;
                }

            base.MoveVExact(move);
        }

        public override void Update()
        {
            base.Update();
            var player = pushPlayer ? PlayerAt(Position) : null;
            if (player != null && player.Speed.Y >= 0 && !player.Data.OnGround &&
                (player.Machine.State != 1 || player.Data.LastClimbMove == -1) &&
                !player.World.Contacts.JumpThruBoostBlocked())
                player.MoveV(40f * engine.DeltaTime);
        }

        public override void Render(ISourceRenderContext context)
        {
            Position += shakeOffset;
            base.Render(context);
            Position -= shakeOffset;
        }
    }
}
