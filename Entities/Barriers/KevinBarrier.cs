using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Display.Colors;
using Celeste.Graphics.Rendering;
using Celeste.Scene.Tracking;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Presentation.Barriers;

namespace Celeste.Mods.MaxHelpingHand.Entities.Barriers
{
    [SceneTracked]
    [CustomEntity("MaxHelpingHand/KevinBarrier")]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.KevinBarrier")]
    public sealed class KevinBarrier : SourceSolidEntityState
    {
        private float flash = 0f;
        private bool flashing = false;

        internal float Solidify = 0f;
        private float solidifyDelay = 0f;
        internal PackedColor Color;
        private PackedColor particleColor;
        private bool flashOnHit;
        internal bool Invisible;

        private List<Vector2> particles = new List<Vector2>();
        private List<KevinBarrier> adjacent = new List<KevinBarrier>();

        private float[] speeds = new float[3] { 12f, 20f, 40f };

        private readonly ISourceEngineContext engine;

        public static KevinBarrier Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new KevinBarrier(data, offset, level.SolidWorldFactory);

        public KevinBarrier(EntityData data, Vector2 offset, ISourceSolidWorldFactory worlds)
            : base(data.Position + offset, data.Width, data.Height, false, worlds.Create(data.Position + offset, false))
        {
            engine = SourceWorld.EngineContext;
            Collidable = false;
            BarrierParticleMotion.Fill(particles, Width, Height, ((ISourceRandomEnvironment)engine).Random);

            Color = SourceHexColor.HexToColor(data.Attr("color", "62222b"));
            particleColor = SourceHexColor.HexToColor(data.Attr("particleColor", "ffffff"));
            flashOnHit = data.Bool("flashOnHit", true);
            Invisible = data.Bool("invisible", false);
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            ((ISourceScene)scene).SourceScene.Entities.GetTrackedEntity<KevinBarrierRenderer>().Track(this);
        }

        public override void Removed(object scene)
        {
            base.Removed(scene);
            ((ISourceScene)scene).SourceScene.Entities.GetTrackedEntity<KevinBarrierRenderer>().Untrack(this);
        }

        public override void Update()
        {
            if (flashing)
            {
                flash = ScalarMath.Approach(flash, 0f, engine.DeltaTime * 4f,
                    ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                if (flash <= 0f)
                {
                    flashing = false;
                }
            }
            else if (solidifyDelay > 0f)
            {
                solidifyDelay -= engine.DeltaTime;
            }
            else if (Solidify > 0f)
            {
                Solidify = ScalarMath.Approach(Solidify, 0f, engine.DeltaTime,
                    ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
            }

            BarrierParticleMotion.AdvanceDown(particles, speeds, Height, engine.DeltaTime);
            base.Update();
        }

        internal void HitByKevin()
        {
            flash = 1f;
            Solidify = 1f;
            solidifyDelay = 1f;
            flashing = true;
            SourceSceneCollision.IntoRect(((ISourceScene)Scene).SourceScene.Entities,
                ((ISourceScene)Scene).SourceScene.Catalog.Find(typeof(KevinBarrier)).Name,
                new IntRect((int)X, (int)Y - 2, (int)Width, (int)Height + 4), adjacent);
            SourceSceneCollision.IntoRect(((ISourceScene)Scene).SourceScene.Entities,
                ((ISourceScene)Scene).SourceScene.Catalog.Find(typeof(KevinBarrier)).Name,
                new IntRect((int)X - 2, (int)Y, (int)Width + 4, (int)Height), adjacent);
            foreach (KevinBarrier item in adjacent)
            {
                if (!item.flashing)
                {
                    item.HitByKevin();
                }
            }

            adjacent.Clear();
        }

        public override void Render(ISourceRenderContext context)
        {
            if (!Invisible)
            {
                PackedColor color = particleColor * 0.5f;
                foreach (Vector2 particle in particles)
                {
                    SourcePrimitiveDrawing.Point(context, this, Position + particle, color);
                }
            }

            if (flashing && flashOnHit)
            {
                SourcePrimitiveDrawing.Rect(context, this, Collider.AbsoluteLeft, Collider.AbsoluteTop, Collider.Width,
                    Collider.Height, PackedColor.White * flash * 0.5f);
            }
        }
    }
}
