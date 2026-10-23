using Celeste.Core.Display;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Tracking.Attributes;
using Celeste.Scene.Scheduling.ExecutionTags;
using Celeste.Simulation.Gameplay.Enemies.Seeker;

namespace Celeste.Mods.MaxHelpingHand.Entities.Barriers
{
    [EverestTrackedAs(typeof(SeekerBarrierRendererState))]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.CustomSeekerBarrier+Renderer")]
    public sealed class CustomSeekerBarrierRenderer : SeekerBarrierRendererState
    {
        internal readonly PackedColor CustomColor;
        internal readonly float CustomTransparency;

        public CustomSeekerBarrierRenderer(ISourceEngineContext engine,
            Celeste.Core.Rendering.ISourceRenderEnvironment environment,
            PackedColor color, float transparency) : base(engine, environment)
        {
            CustomColor = color;
            CustomTransparency = transparency;
            Tags = ((ISourcePersistentTransitionExecutionTagsEnvironment)engine).PersistentTransitionExecutionTags
                .TransitionUpdate.Value;
            Depth = 1;
        }
    }
}
