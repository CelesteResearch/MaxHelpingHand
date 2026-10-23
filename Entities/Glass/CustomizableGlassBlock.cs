// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Rendering;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Walls.Solids;
using Celeste.Graphics.Rendering;
using Celeste.Physics.Collision;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.MaxHelpingHand.Entities.Glass
{
    [CustomEntity("MaxHelpingHand/CustomizableGlassBlock")]
    [SceneTracked(inherited: true)]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.CustomizableGlassBlock")]
    public class CustomizableGlassBlock : GlassBlockState
    {
        public float Alpha { get; protected set; } = 1f;
        public Vector2 ShakeVector { get; protected set; } = Vector2.Zero;
        protected virtual bool FallingGlass => false;
        private readonly List<Func<float>> lineAlpha = new();

        public static CustomizableGlassBlock Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset,
            OriginalLevelEntityData data) =>
            new(data.Position + offset, data.Width, data.Height, data.Bool("behindFgTiles"),
                level.SolidWorldFactory.Create(data.Position + offset, false));

        public CustomizableGlassBlock(Vector2 position, float width, float height, bool behindFgTiles,
            ISourceSolidWorld world) : base(position, width, height, false, world)
        {
            Depth = behindFgTiles ? -9995 : -10000;
        }

        protected override Vector2 OutlineOffset(Vector2 normal) => Vector2.Zero;

        protected override bool Open(Vector2 tile)
        {
            if (FallingGlass) return true;
            var point = new Vector2(X + tile.X * 8f + 4f, Y + tile.Y * 8f + 4f);
            var entities = ((ISourceScene)Scene).SourceScene.Entities;
            foreach (ISourceEntityCollider solid in entities.GetTrackedEntities("Celeste.SolidTiles"))
                if (solid.Collidable && solid.Collider != null && solid.Collider.Collide(point))
                    return false;
            foreach (CustomizableGlassBlock block in entities.GetTrackedEntities(typeof(CustomizableGlassBlock)))
                if (block.Collidable && block.Collider != null && block.Collider.Collide(point))
                    return block.FallingGlass;
            return true;
        }

        internal void GlassExitBlockSolidified(CustomizableGlassBlock exitBlock)
        {
            var previous = Lines.ToArray();
            Lines.Clear();
            lineAlpha.Clear();
            ComputeLines();
            for (int i = 0; i < Lines.Count; i++) lineAlpha.Add(() => exitBlock.Alpha);
            foreach (var line in previous)
            {
                Lines.Add(line);
                lineAlpha.Add(() => 1f - exitBlock.Alpha);
            }
        }

        public override void Render(ISourceRenderContext context)
        {
            for (int i = 0; i < Lines.Count; i++)
            {
                var line = Lines[i];
                float fade = lineAlpha.Count == 0 ? 1f : lineAlpha[i]();
                SourcePrimitiveDrawing.Line(context, this, Position + ShakeVector + line.A,
                    Position + ShakeVector + line.B,
                    LineColor * fade * Alpha, ((ISourceVectorEnvironment)SourceWorld.EngineContext).VectorArithmetic);
            }
        }
    }
}
