using System;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Rendering;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Tracking.Attributes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Display;
using Celeste.Mods.MaxHelpingHand.Components.Platforms;
using Celeste.Mods.MaxHelpingHand.Runtime.Hazards.Bumpers;
using Celeste.Mods.MaxHelpingHand.Runtime.Platforms.Movement;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.MaxHelpingHand.Entities.Platforms
{
    [CustomEntity("MaxHelpingHand/SidewaysMovingPlatform=Load")]
    [EverestTrackedAs(typeof(SidewaysJumpThru))]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.SidewaysMovingPlatform")]
    public class SidewaysMovingPlatform : SidewaysJumpThru
    {
        private readonly EntityData thisEntityData;
        private readonly Vector2 thisOffset;
        private readonly ISourceSolidWorldFactory worlds;
        private readonly ISourceEngineContext sourceEngine;
        private readonly string texture;
        private readonly bool left;
        private AtlasEntry[] textures;
        private readonly SourceSolidEntityState playerInteractingSolid;
        private MultiNodeMovingPlatform animatingPlatform;
        private bool spawnedByOtherPlatform;

        public static SidewaysMovingPlatform Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
            => new(level.EngineContext, data, offset, ((ISourcePlatformCollisionScene)level).SolidWorldFactory);

        public SidewaysMovingPlatform(ISourceEngineContext engine, EntityData data, Vector2 offset,
            ISourceSolidWorldFactory worlds)
            : base(engine, data.Position + offset, data.Height, !data.Bool("left"), "MaxHelpingHand/invisible", 0f,
                true, true, true, -1, false, false)
        {
            thisEntityData = data;
            thisOffset = offset;
            this.worlds = worlds;
            sourceEngine = engine;
            texture = data.Attr("texture", "default");
            left = data.Bool("left");
            playerInteractingSolid =
                new SourceSolidEntityState(Position, Width, Height, false, worlds.Create(Position, false))
                    { Collidable = false, Visible = false };
            if (!left) playerInteractingSolid.Position += Vector2.UnitX * 3f;
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            var scheduler = ((ISourceScene)scene).SourceScene;
            scheduler.QueueSourceEntity(playerInteractingSolid);
            AtlasEntry fullTexture =
                ((ISourceGameGraphicsEnvironment)sourceEngine).GameGraphics
                .GameAtlas["objects/woodPlatform/" + texture];
            textures = new AtlasEntry[fullTexture.FrameWidth / 8];
            for (int i = 0; i < textures.Length; i++) textures[i] = fullTexture.GetSubtexture(i * 8, 0, 8, 8);
            if (spawnedByOtherPlatform) return;
            var mover = CreateMover();
            Add(mover);
            animatingPlatform = new MultiNodeMovingPlatform(thisEntityData, thisOffset, worlds, otherPlatform =>
            {
                if (otherPlatform != animatingPlatform)
                {
                    var other = new SidewaysMovingPlatform(sourceEngine, thisEntityData, thisOffset, worlds)
                        { spawnedByOtherPlatform = true };
                    ((ISourceScene)Scene).SourceScene.QueueSourceEntity(other);
                    var otherMover = other.CreateMover();
                    other.Add(otherMover);
                    MovingPlatformAttachment.Animate(otherPlatform, otherMover,
                        ((ISourceVectorEnvironment)sourceEngine).VectorArithmetic.Divide(
                            new Vector2(Width + 4f, Height), 2f));
                }
            });
            MovingPlatformAttachment.Animate(animatingPlatform, mover,
                ((ISourceVectorEnvironment)sourceEngine).VectorArithmetic.Divide(new Vector2(Width + 4f, Height), 2f));
            scheduler.QueueSourceEntity(animatingPlatform);
        }

        private StaticMoverWithLiftSpeed CreateMover() => new()
        {
            OnMove = move => SourceSideWallPlatformMovement.Move(this, playerInteractingSolid, left, move),
            OnSetLiftSpeed = liftSpeed => playerInteractingSolid.LiftSpeed = liftSpeed
        };

        public override void Render(ISourceRenderContext context)
        {
            base.Render(context);
            if (left)
            {
                float rotation = (float)(-Math.PI / 2);
                Draw(context, 0, Position, new Vector2(Height, 0), rotation);
                for (int i = 16; i < Height; i += 8)
                    Draw(context, 1, Position + new Vector2(0, i), Vector2.Zero, rotation);
                Draw(context, 3, Position, new Vector2(8, 0), rotation);
                Draw(context, 2, Position + new Vector2(0, Height / 2f + 4f), Vector2.Zero, rotation);
            }
            else
            {
                float rotation = (float)(Math.PI / 2);
                Draw(context, 0, Position, new Vector2(0, 8), rotation);
                for (int i = 8; i < Height - 8f; i += 8)
                    Draw(context, 1, Position + new Vector2(8, i), Vector2.Zero, rotation);
                Draw(context, 3, Position + new Vector2(8, Height - 8f), Vector2.Zero, rotation);
                Draw(context, 2, Position + new Vector2(8, Height / 2f - 4f), Vector2.Zero, rotation);
            }
        }

        private void Draw(ISourceRenderContext context, int index, Vector2 at, Vector2 origin, float rotation)
        {
            var draw = new AtlasTextureDraw(textures[index], at, origin, PackedColor.White, Vector2.One, rotation);
            ((ISourceRenderCommandSink<AtlasTextureDraw>)context).Draw(in draw, this);
        }
    }
}
