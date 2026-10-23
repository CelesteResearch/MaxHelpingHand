using System.Linq;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Tracking.Attributes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Display.Colors;
using Celeste.Mods.MaxHelpingHand.Runtime.Barriers;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Enemies.Seeker;

namespace Celeste.Mods.MaxHelpingHand.Entities.Barriers
{
    [CustomEntity("MaxHelpingHand/CustomSeekerBarrier")]
    [EverestTrackedAs(typeof(SeekerBarrierState))]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.CustomSeekerBarrier")]
    public sealed class CustomSeekerBarrier : SeekerBarrierState
    {
        private readonly CustomSeekerBarrierRenderer renderer;
        internal readonly PackedColor CustomParticleColor;
        internal readonly float CustomParticleTransparency;
        internal readonly float ParticleDirection;
        internal readonly bool Wavy;

        private readonly bool killSeekers,
            killJellyfish,
            killHoldableContainerNonSlowFall,
            killHoldableContainerSlowFall;

        private readonly string disableIfFlag;
        private bool isDisabled;

        public static CustomSeekerBarrier Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(level, data, offset);

        private CustomSeekerBarrier(OriginalLevelSceneRoot level, EntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height,
                level.SolidWorldFactory.Create(data.Position + offset, false))
        {
            var engine = level.EngineContext;
            if (((ISourceSeekerBarrierBehaviorEnvironment)engine).SeekerBarrierBehavior.ReadMapStaticMovers)
                AllowStaticMovers = data.Bool("allowStaticMovers", true);
            renderer = new CustomSeekerBarrierRenderer(engine, (Celeste.Core.Rendering.ISourceRenderEnvironment)engine,
                SourceHexColor.HexToColor(data.Attr("color", "FFFFFF")), data.Float("transparency", .15f));
            CustomParticleColor = SourceHexColor.HexToColor(data.Attr("particleColor", "FFFFFF"));
            CustomParticleTransparency = data.Float("particleTransparency", .5f);
            ParticleDirection = data.Float("particleDirection");
            Wavy = data.Bool("wavy", true);
            killSeekers = data.Bool("killSeekers", true);
            killJellyfish = data.Bool("killJellyfish", true);
            disableIfFlag = data.Attr("disableIfFlag");
            killHoldableContainerNonSlowFall = data.Bool("killHoldableContainerNonSlowFall", true);
            killHoldableContainerSlowFall = data.Bool("killHoldableContainerSlowFall", true);
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            var scheduler = ((ISourceScene)scene).SourceScene;
            scheduler.Entities.GetTrackedEntity<SeekerBarrierRendererState>("Celeste.SeekerBarrierRenderer")
                .Untrack(this);
            scheduler.AddEntity(renderer);
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            renderer.Track(this);
            var process = SeekerBarrierProcess.For(BarrierEngineContext);
            var scheduler = ((ISourceScene)scene).SourceScene;
            if (process.Next == null && !scheduler.GetToAdd().OfType<SeekerBarrierColorController>().Any())
                scheduler.AddEntity(new SeekerBarrierColorController(BarrierEngineContext, new EntityData(),
                    Vector2.Zero));
        }

        public override void Update()
        {
            base.Update();
            if (string.IsNullOrEmpty(disableIfFlag)) return;
            bool flag = ((OriginalLevelSceneRoot)Scene).Session.GetFlag(disableIfFlag);
            if (!isDisabled && flag)
            {
                isDisabled = true;
                renderer.Untrack(this);
                Visible = false;
            }
            else if (isDisabled && !flag)
            {
                isDisabled = false;
                renderer.Track(this);
                Visible = true;
            }
        }

        public override bool CollidesWithSeeker(bool original) => killSeekers && !isDisabled;
        public override bool CollidesWithGlider(bool original) => killJellyfish && !isDisabled;

        public override bool CollidesWithHoldable(bool original, bool slowFall)
            => !isDisabled && (slowFall ? killHoldableContainerSlowFall : killHoldableContainerNonSlowFall);

        public override bool BloomMaskAllowed() => !isDisabled && base.BloomMaskAllowed();
    }
}
