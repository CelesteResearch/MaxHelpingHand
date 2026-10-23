using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Runtime;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.MaxHelpingHand.Entities.Barriers;
using Celeste.Mods.MaxHelpingHand.Modules;
using Celeste.Simulation.Gameplay.Enemies.Seeker;
using Celeste.Simulation.Gameplay.Player.Intro;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Runtime.Barriers
{
    public sealed class SeekerBarrierProcess : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly List<IDisposable> hooks = new();
        internal readonly MaxHelpingHandModule Module;

        private readonly HashSet<string> controllerNames = new(StringComparer.Ordinal)
        {
            "MaxHelpingHand/SeekerBarrierColorController", "MaxHelpingHand/SeekerBarrierColorControllerDisabler"
        };

        public void RegisterPersistentControllerName(string name) => controllerNames.Add(name);
        public void UnregisterPersistentControllerName(string name) => controllerNames.Remove(name);
        internal SeekerBarrierColorController Current, Next;
        internal float TransitionProgress = -1f;
        internal bool Enabled, AllowedToRenderBloom;

        internal SeekerBarrierProcess(ISourceEngineContext engine, MaxHelpingHandModule module)
        {
            this.engine = engine;
            Module = module;
        }

        internal static SeekerBarrierProcess For(ISourceEngineContext engine)
            => ((ISourceEverestModulesEnvironment)engine).Modules.OfType<MaxHelpingHandModule>().Single()
                .SeekerBarriers;

        internal void Load()
        {
            try
            {
                hooks.Add(new Hook(typeof(OriginalLevelSceneRoot).GetMethod(nameof(OriginalLevelSceneRoot.LoadLevel)),
                    (Action<Action<OriginalLevelSceneRoot, PlayerIntroType, bool>, OriginalLevelSceneRoot,
                        PlayerIntroType, bool>)RestoreController));
                hooks.Add(new Hook(typeof(SeekerBarrierState).GetMethod(nameof(SeekerBarrierState.ParticleColor)),
                    (Func<Func<SeekerBarrierState, PackedColor>, SeekerBarrierState, PackedColor>)ParticleColor));
                hooks.Add(new Hook(
                    typeof(SeekerBarrierState).GetMethod(nameof(SeekerBarrierState.ParticleTransparency)),
                    (Func<Func<SeekerBarrierState, float>, SeekerBarrierState, float>)ParticleTransparency));
                hooks.Add(new Hook(
                    typeof(SeekerBarrierRendererState).GetMethod(nameof(SeekerBarrierRendererState.BarrierColor)),
                    (Func<Func<SeekerBarrierRendererState, PackedColor>, SeekerBarrierRendererState, PackedColor>)
                    BarrierColor));
                hooks.Add(new Hook(
                    typeof(SeekerBarrierRendererState).GetMethod(nameof(SeekerBarrierRendererState
                        .BarrierTransparency)),
                    (Func<Func<SeekerBarrierRendererState, float>, SeekerBarrierRendererState, float>)
                    BarrierTransparency));
                hooks.Add(new Hook(
                    typeof(SeekerBarrierState).GetMethod(nameof(SeekerBarrierState.AdvanceParticleMotion)),
                    (Action<Action<SeekerBarrierState>, SeekerBarrierState>)AdvanceParticles));
                hooks.Add(new Hook(typeof(SeekerBarrierState).GetMethod(nameof(SeekerBarrierState.Update)),
                    (Action<Action<SeekerBarrierState>, SeekerBarrierState>)UpdateBarrier));
                hooks.Add(new Hook(
                    typeof(SeekerBarrierRendererState).GetMethod(nameof(SeekerBarrierRendererState.OnRenderBloom)),
                    (Action<Action<SeekerBarrierRendererState>, SeekerBarrierRendererState>)RenderBloom));
                hooks.Add(new Hook(typeof(SeekerBarrierState).GetMethod(nameof(SeekerBarrierState.BloomMaskAllowed)),
                    (Func<Func<SeekerBarrierState, bool>, SeekerBarrierState, bool>)BloomMaskAllowed));
                hooks.Add(new Hook(
                    typeof(SeekerBarrierRendererState).GetMethod(nameof(SeekerBarrierRendererState.Render)),
                    (Action<Action<SeekerBarrierRendererState, ISourceRenderContext>, SeekerBarrierRendererState,
                        ISourceRenderContext>)Render));
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
            hooks.Clear();
            Current?.DisposeBuffer();
            Next?.DisposeBuffer();
            Current = Next = null;
            Enabled = AllowedToRenderBloom = false;
            TransitionProgress = -1f;
        }

        internal void PromoteNext()
        {
            if (TransitionProgress != -1f || Current != null) return;
            Current = Next;
            Next = null;
        }

        private void RestoreController(Action<OriginalLevelSceneRoot, PlayerIntroType, bool> orig,
            OriginalLevelSceneRoot level, PlayerIntroType intro, bool fromLoader)
        {
            if (ReferenceEquals(level.EngineContext, engine) && Module.Session.SeekerBarrierCurrentColors is { } state
                                                             && level.Session.LevelData is OriginalLevelRoomData room &&
                                                             !room.Entities.Any(entity =>
                                                                 controllerNames.Contains(entity.Name)))
                level.SourceScene.AddEntity(new SeekerBarrierColorController(engine, state.Restore(), Vector2.Zero));
            orig(level, intro, fromLoader);
        }

        internal T GetAndLerp<T>(Func<SeekerBarrierColorController, T> getter, T defaultValue,
            Func<T, T, float, T> lerp)
        {
            if (TransitionProgress == -1f)
            {
                PromoteNext();
                return Current == null ? defaultValue : getter(Current);
            }

            return lerp(Current == null ? defaultValue : getter(Current),
                Next == null ? defaultValue : getter(Next), TransitionProgress);
        }

        private bool Owns(SeekerBarrierState barrier) => ReferenceEquals(barrier.BarrierEngineContext, engine);

        private bool Owns(SeekerBarrierRendererState renderer) =>
            ReferenceEquals(renderer.BarrierEngineContext, engine);

        private float Lerp(float from, float to, float progress)
            => ((Celeste.Core.Mathematics.ISourceScalarEnvironment)engine).ScalarArithmetic.Lerp(from, to, progress);

        private PackedColor ParticleColor(Func<SeekerBarrierState, PackedColor> orig, SeekerBarrierState barrier)
            => !Enabled || !Owns(barrier)
                ? orig(barrier)
                : barrier is CustomSeekerBarrier custom
                    ? custom.CustomParticleColor
                    : GetAndLerp(c => c.ParticleColor, PackedColor.White, PackedColor.Lerp);

        private float ParticleTransparency(Func<SeekerBarrierState, float> orig, SeekerBarrierState barrier)
            => !Enabled || !Owns(barrier)
                ? orig(barrier)
                : barrier is CustomSeekerBarrier custom
                    ? custom.CustomParticleTransparency
                    : GetAndLerp(c => c.ParticleTransparency, .5f, Lerp);

        private PackedColor BarrierColor(Func<SeekerBarrierRendererState, PackedColor> orig,
            SeekerBarrierRendererState renderer)
            => !Enabled || !Owns(renderer)
                ? orig(renderer)
                : renderer is CustomSeekerBarrierRenderer custom
                    ? custom.CustomColor
                    : GetAndLerp(c => c.Color, PackedColor.White, PackedColor.Lerp);

        private float BarrierTransparency(Func<SeekerBarrierRendererState, float> orig,
            SeekerBarrierRendererState renderer)
            => !Enabled || !Owns(renderer)
                ? orig(renderer)
                : renderer is CustomSeekerBarrierRenderer custom
                    ? custom.CustomTransparency
                    : GetAndLerp(c => c.Transparency, .15f, Lerp);

        private void Parameters(SeekerBarrierState barrier, out float direction, out bool wavy)
        {
            direction = Current?.ParticleDirection ?? 0f;
            wavy = Current?.Wavy ?? true;
            if (barrier is not CustomSeekerBarrier custom) return;
            direction = custom.ParticleDirection;
            wavy = custom.Wavy;
        }

        private void AdvanceParticles(Action<SeekerBarrierState> orig, SeekerBarrierState barrier)
        {
            if (!Enabled || !Owns(barrier))
            {
                orig(barrier);
                return;
            }

            Parameters(barrier, out float direction, out bool wavy);
            if (direction == 0f && wavy)
            {
                orig(barrier);
                return;
            }

            float angle = (float)(direction * Math.PI / 180);
            barrier.AdvanceWrappedParticleMotion(
                ((Celeste.Core.Mathematics.ISourceVectorEnvironment)engine).VectorArithmetic.Rotate(Vector2.UnitY,
                    angle));
        }

        private void UpdateBarrier(Action<SeekerBarrierState> orig, SeekerBarrierState barrier)
        {
            orig(barrier);
            if (!Enabled || !Owns(barrier)) return;
            Parameters(barrier, out _, out bool wavy);
            if (!wavy) barrier.Solidify = 1f;
        }

        private void RenderBloom(Action<SeekerBarrierRendererState> orig, SeekerBarrierRendererState renderer)
        {
            if (!Enabled || !Owns(renderer) ||
                ((Current?.RenderBloom ?? true) &&
                 (!(Current?.BarrierDepth.HasValue ?? false) || AllowedToRenderBloom)))
                orig(renderer);
        }

        private bool BloomMaskAllowed(Func<SeekerBarrierState, bool> orig, SeekerBarrierState barrier)
            => (!Enabled || !Owns(barrier) ||
                (!(Current?.BarrierDepth.HasValue ?? false) && (Current?.RenderBloom ?? true)))
               && orig(barrier);

        private void Render(Action<SeekerBarrierRendererState, ISourceRenderContext> orig,
            SeekerBarrierRendererState renderer, ISourceRenderContext context)
        {
            orig(renderer, context);
            if (!Enabled || !Owns(renderer)) return;
            if ((Current?.RenderBloom ?? true) && (Current?.BarrierDepth.HasValue ?? false) &&
                Current.Scene is OriginalLevelSceneRoot level)
                Current.RenderDepthBloom(level);
        }
    }
}
