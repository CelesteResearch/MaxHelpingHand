using System;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Scene.Tracking;
using Celeste.Mods.MaxHelpingHand.Entities.Backdrops;
using Celeste.Simulation.Presentation.Backdrops.Parallax;
using Celeste.Simulation.Presentation.Backdrops.Runtime;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Runtime.Backdrops
{
    internal sealed class ParallaxFadeSpeedHooks : IDisposable
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, ParallaxFadeSpeedHooks> owners = new();
        private readonly ISourceEngineContext engine;
        private readonly ILHook hook;
        internal readonly string Key;
        internal bool Enabled;

        internal static ParallaxFadeSpeedHooks For(ISourceEngineContext engine) =>
            owners.GetValue(engine, e => new ParallaxFadeSpeedHooks(e));

        private ParallaxFadeSpeedHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            Key = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog.Find(typeof(ParallaxFadeSpeedController))
                .RegistryKey;
            hook = new ILHook(
                typeof(ParallaxBackdropPresentation).GetMethod(nameof(ParallaxBackdropPresentation.Update)), Patch);
        }

        private void Patch(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(i =>
                i.MatchLdfld<ParallaxBackdropPresentation>(nameof(ParallaxBackdropPresentation.DoFadeIn)));
            cursor.GotoNext(MoveType.After,
                i => i.MatchLdfld<SourceEngineFrameState>(nameof(SourceEngineFrameState.DeltaTime)));
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Func<float, ISourceBackdropScene, float>>(FadeSpeed);
        }

        internal float FadeSpeed(float original, ISourceBackdropScene scene)
        {
            if (!Enabled || !ReferenceEquals(scene.EngineContext, engine)) return original;
            var controllers = scene.SourceScene.Entities.GetTrackedEntities(Key);
            return controllers.Count == 0
                ? original
                : original / ((ParallaxFadeSpeedController)controllers[0]).FadeTime;
        }

        public void Dispose()
        {
            hook.Dispose();
            owners.Remove(engine);
        }
    }
}
