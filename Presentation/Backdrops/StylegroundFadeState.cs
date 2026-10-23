using System;
using System.Collections.Generic;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.MaxHelpingHand.Entities.Backdrops;
using Celeste.Simulation.Presentation.Backdrops.Runtime;

namespace Celeste.Mods.MaxHelpingHand.Presentation.Backdrops
{
    internal sealed class StylegroundFadeState : IDisposable
    {
        internal readonly OriginalLevelSceneRoot Level;
        private readonly HashSet<StylegroundFadeController> controllers = new();
        private readonly Dictionary<(string Flag, bool NotFlag), FadeEntry> entries = new();
        private IDisposable backgroundOpacity;
        private IDisposable foregroundOpacity;

        internal StylegroundFadeState(OriginalLevelSceneRoot level) => Level = level;
        internal bool Empty => controllers.Count == 0;
        internal bool Contains(StylegroundFadeController controller) => controllers.Contains(controller);

        internal void Register(StylegroundFadeController controller)
        {
            if (entries.Count == 0)
            {
                backgroundOpacity = Level.Background.AddOpacityPolicy(GetOpacity, 320, 180);
                foregroundOpacity = Level.Foreground.AddOpacityPolicy(GetOpacity, 320, 180);
            }

            foreach (string flag in controller.Flags)
                entries[(flag, controller.NotFlag)] = new FadeEntry(controller,
                    Level.Session.GetFlag(flag) != controller.NotFlag ? 1 : 0);
            controllers.Add(controller);
        }

        internal void Deregister(StylegroundFadeController controller)
        {
            foreach (string flag in controller.Flags)
            {
                var key = (flag, controller.NotFlag);
                if (entries.TryGetValue(key, out var entry) && ReferenceEquals(entry.Controller, controller))
                    entries.Remove(key);
            }

            controllers.Remove(controller);
            if (entries.Count != 0) return;
            backgroundOpacity?.Dispose();
            foregroundOpacity?.Dispose();
            backgroundOpacity = foregroundOpacity = null;
        }

        internal void Advance()
        {
            float deltaTime = Level.EngineContext.DeltaTime;
            var arithmetic = ((ISourceExtremaEnvironment)Level.EngineContext).ExtremaArithmetic;
            foreach (var pair in entries)
            {
                var entry = pair.Value;
                bool enabled = Level.Session.GetFlag(pair.Key.Flag) != pair.Key.NotFlag;
                entry.Fade = ScalarMath.Approach(entry.Fade, enabled ? 1 : 0,
                    deltaTime / (enabled ? entry.Controller.FadeInTime : entry.Controller.FadeOutTime), arithmetic);
            }
        }

        internal bool ForceVisible(BackdropPresentation backdrop)
            => backdrop.OnlyIfFlag != null && entries.TryGetValue((backdrop.OnlyIfFlag, false), out var positive) &&
               positive.Fade > 0 ||
               backdrop.OnlyIfNotFlag != null &&
               entries.TryGetValue((backdrop.OnlyIfNotFlag, true), out var negative) && negative.Fade > 0;

        private float? GetOpacity(BackdropPresentation backdrop, ISourceBackdropScene scene)
        {
            float? fade = null;
            if (backdrop.OnlyIfFlag != null && entries.TryGetValue((backdrop.OnlyIfFlag, false), out var positive) &&
                positive.Fade < 1)
                fade = positive.Fade;
            if (backdrop.OnlyIfNotFlag != null &&
                entries.TryGetValue((backdrop.OnlyIfNotFlag, true), out var negative) && negative.Fade < 1)
                fade = negative.Fade;
            return fade;
        }

        public void Dispose()
        {
            backgroundOpacity?.Dispose();
            foregroundOpacity?.Dispose();
            backgroundOpacity = foregroundOpacity = null;
            entries.Clear();
            controllers.Clear();
        }

        private sealed class FadeEntry
        {
            internal readonly StylegroundFadeController Controller;
            internal float Fade;

            internal FadeEntry(StylegroundFadeController controller, float fade)
            {
                Controller = controller;
                Fade = fade;
            }
        }
    }
}
