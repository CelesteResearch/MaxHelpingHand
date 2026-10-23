using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Graphics.Content.VirtualAssets;
using Celeste.Graphics.Display.Colors;
using Celeste.Mods.MaxHelpingHand.Runtime.Barriers;
using Celeste.Mods.MaxHelpingHand.Sessions;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Enemies.Seeker;

namespace Celeste.Mods.MaxHelpingHand.Entities.Barriers
{
    [CustomEntity("MaxHelpingHand/SeekerBarrierColorController")]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.SeekerBarrierColorController")]
    public sealed partial class SeekerBarrierColorController : SceneEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly SeekerBarrierProcess process;
        internal readonly PackedColor Color, ParticleColor;
        internal readonly float Transparency, ParticleTransparency, ParticleDirection;
        internal readonly int? BarrierDepth;
        internal readonly bool Wavy, RenderBloom;
        private readonly int entityID;
        private VirtualRenderTarget levelRenderTarget;

        public SeekerBarrierColorController(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            process = SeekerBarrierProcess.For(engine);
            Color = SourceHexColor.HexToColor(data.Attr("color", "FFFFFF"));
            ParticleColor = SourceHexColor.HexToColor(data.Attr("particleColor", "FFFFFF"));
            Transparency = data.Float("transparency", .15f);
            ParticleTransparency = data.Float("particleTransparency", .5f);
            ParticleDirection = data.Float("particleDirection");
            Wavy = data.Bool("wavy", true);
            RenderBloom = data.Bool("renderBloom", true);
            entityID = data.ID;
            if (int.TryParse(data.Attr("depth"), out int depth))
            {
                BarrierDepth = depth;
                EnsureBuffer();
            }

            OwnedComponents.Add(new RoomTransitionListener
            {
                OnIn = progress => process.TransitionProgress = progress,
                OnOut = progress => process.TransitionProgress = progress,
                OnInBegin = () => process.TransitionProgress = 0f,
                OnInEnd = () => process.TransitionProgress = -1f
            });
            process.Module.Session.SeekerBarrierCurrentColors = data.Bool("persistent")
                ? new SeekerBarrierColorState
                {
                    Color = data.Attr("color", "FFFFFF"), ParticleColor = data.Attr("particleColor", "FFFFFF"),
                    Transparency = Transparency, ParticleTransparency = ParticleTransparency,
                    ParticleDirection = ParticleDirection, Depth = BarrierDepth, Wavy = Wavy, RenderBloom = RenderBloom
                }
                : null;
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            process.Next = this;
            process.Enabled = true;
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            if (!BarrierDepth.HasValue) return;
            var entities = ((ISourceScene)scene).SourceScene.Entities;
            foreach (SeekerBarrierState barrier in entities.GetTrackedEntities("Celeste.SeekerBarrier"))
                barrier.Depth = BarrierDepth.Value;
            foreach (SeekerBarrierRendererState renderer in
                     entities.GetTrackedEntities("Celeste.SeekerBarrierRenderer"))
                renderer.Depth = BarrierDepth.Value + 1;
        }

        public override void Removed(object scene)
        {
            base.Removed(scene);
            process.Current = process.Next;
            process.Next = null;
            process.TransitionProgress = -1f;
            if (process.Current == null) process.Enabled = false;
            DisposeBuffer();
        }

        public override void SceneEnd(object scene)
        {
            base.SceneEnd(scene);
            process.Current = process.Next = null;
            process.Enabled = false;
            DisposeBuffer();
        }

        public override void Update()
        {
            base.Update();
            process.PromoteNext();
        }

        internal void DisposeBuffer()
        {
            levelRenderTarget?.Dispose();
            levelRenderTarget = null;
        }

        private void EnsureBuffer()
        {
            var buffers = ((Celeste.Game.Presentation.Effects.Bloom.IOriginalBloomProcess)engine).Graphics.Buffers;
            int width = buffers.Gameplay.Width, height = buffers.Gameplay.Height;
            if (levelRenderTarget != null && levelRenderTarget.Width == width &&
                levelRenderTarget.Height == height) return;
            levelRenderTarget?.Dispose();
            levelRenderTarget = ((ISourceVirtualContentEnvironment)engine).VirtualContent.CreateRenderTarget(
                "helping-hand-seeker-barrier-color-controller-" + entityID, width, height);
        }
    }
}
