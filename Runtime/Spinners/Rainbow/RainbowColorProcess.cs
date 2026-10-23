using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Entities.Hazards.Spinners.Crystal;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.MaxHelpingHand.Entities.Spinners.Rainbow;
using Celeste.Mods.MaxHelpingHand.Modules;
using Celeste.Physics.Collision;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Hazards.Spinners.Crystal;
using Celeste.Simulation.Gameplay.Player.Intro;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Runtime.Spinners.Rainbow
{
    public sealed class RainbowColorProcess : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly MaxHelpingHandModule module;
        private readonly List<IDisposable> hooks = new();

        private readonly HashSet<string> controllerNames = new(StringComparer.Ordinal)
        {
            "MaxHelpingHand/RainbowSpinnerColorController", "MaxHelpingHand/FlagRainbowSpinnerColorController",
            "MaxHelpingHand/RainbowSpinnerColorControllerDisabler"
        };

        internal RainbowSpinnerColorController Current;
        internal RainbowSpinnerColorController Next;
        internal float TransitionProgress = -1;
        internal bool Enabled;
        internal bool AreaEnabled;

        public RainbowColorProcess(ISourceEngineContext engine, MaxHelpingHandModule module)
        {
            this.engine = engine;
            this.module = module;
        }

        internal void Load()
        {
            try
            {
                var method =
                    typeof(OriginalCrystalStaticSpinner).GetMethod(nameof(OriginalCrystalStaticSpinner.GetHue));
                hooks.Add(new Hook(method,
                    (Func<Func<OriginalCrystalStaticSpinner, Vector2, PackedColor>, OriginalCrystalStaticSpinner,
                        Vector2, PackedColor>)GetSpinnerHue));
                using (new DetourConfigContext(
                           new DetourConfig("MaxHelpingHand.RainbowArea").WithPriority(int.MaxValue)).Use())
                    hooks.Add(new Hook(method,
                        (Func<Func<OriginalCrystalStaticSpinner, Vector2, PackedColor>, OriginalCrystalStaticSpinner,
                            Vector2, PackedColor>)GetAreaSpinnerHue));
                hooks.Add(new Hook(typeof(OriginalLevelSceneRoot).GetMethod(nameof(OriginalLevelSceneRoot.LoadLevel)),
                    (Action<Action<OriginalLevelSceneRoot, PlayerIntroType, bool>, OriginalLevelSceneRoot,
                        PlayerIntroType, bool>)LevelLoaded));
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        public void Dispose()
        {
            for (var i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
            hooks.Clear();
            ResetControllers();
            AreaEnabled = false;
        }

        internal void ResetControllers(bool resetTransition = true)
        {
            Current = Next = null;
            Enabled = false;
            if (resetTransition) TransitionProgress = -1;
        }

        internal void PromoteNext()
        {
            if (TransitionProgress != -1 || Current != null) return;
            Current = Next;
            Next = null;
        }

        public void RegisterPersistentControllerName(string name) => controllerNames.Add(name);
        public void UnregisterPersistentControllerName(string name) => controllerNames.Remove(name);

        private void LevelLoaded(Action<OriginalLevelSceneRoot, PlayerIntroType, bool> orig,
            OriginalLevelSceneRoot level, PlayerIntroType intro, bool fromLoader)
        {
            orig(level, intro, fromLoader);
            if (!ReferenceEquals(level.EngineContext, engine) || !ShouldRestore(level)) return;
            var controller = new RainbowSpinnerColorController(engine, module,
                module.Session.RainbowSpinnerCurrentColors.Restore(), Vector2.Zero);
            level.SourceScene.AddEntity(controller);
            level.SourceScene.CommitQueuedEntityLists();
        }

        internal bool ShouldRestore(OriginalLevelSceneRoot level)
        {
            if (module.Session.RainbowSpinnerCurrentColors == null ||
                level.Session.LevelData is not OriginalLevelRoomData room) return false;
            foreach (var entity in room.Entities)
                if (controllerNames.Contains(entity.Name))
                    return false;
            return true;
        }

        internal void UpdateRainbowSpinners(object scene)
        {
            var spinners =
                ((ISourceScene)scene).SourceScene.Entities.GetTrackedEntities(typeof(OriginalCrystalStaticSpinner));
            foreach (OriginalCrystalStaticSpinner spinner in spinners)
                if (spinner.SpinnerColor == CrystalSpinnerColor.Rainbow)
                    spinner.UpdateHue();
        }

        private bool Owns(OriginalCrystalStaticSpinner spinner)
            => ReferenceEquals(((ISourceEngineContextEntity)spinner).EngineContext, engine);

        private PackedColor GetSpinnerHue(Func<OriginalCrystalStaticSpinner, Vector2, PackedColor> orig,
            OriginalCrystalStaticSpinner spinner, Vector2 position)
        {
            if (!Enabled || !Owns(spinner)) return orig(spinner, position);
            PromoteNext();
            if (TransitionProgress == -1)
                return Current == null ? orig(spinner, position) : Current.Settings.GetHue(spinner.Scene, position);
            var from = Current == null ? orig(spinner, position) : Current.Settings.GetHue(spinner.Scene, position);
            var to = Next == null ? orig(spinner, position) : Next.Settings.GetHue(spinner.Scene, position);
            return PackedColor.Lerp(from, to, TransitionProgress);
        }

        private PackedColor GetAreaSpinnerHue(Func<OriginalCrystalStaticSpinner, Vector2, PackedColor> orig,
            OriginalCrystalStaticSpinner spinner, Vector2 position)
        {
            if (!AreaEnabled || !Owns(spinner)) return orig(spinner, position);
            var areas =
                ((ISourceScene)spinner.Scene).SourceScene.Entities.GetTrackedEntities(
                    typeof(RainbowSpinnerColorAreaController));
            foreach (RainbowSpinnerColorAreaController controller in areas)
                if (SourceEntityCollision.CheckAt(spinner, controller, position))
                    return controller.Settings.GetHue(spinner.Scene, position);
            return orig(spinner, position);
        }

        // Jungle's original rainbow decal consumer uses point inclusion rather than spinner-shape overlap.
        public bool TryGetAreaPointHue(object scene, Vector2 position, out PackedColor color)
        {
            if (AreaEnabled)
            {
                var areas = ((ISourceScene)scene).SourceScene.Entities.GetTrackedEntities(
                    typeof(RainbowSpinnerColorAreaController));
                foreach (RainbowSpinnerColorAreaController controller in areas)
                    if (controller.Collider.Collide(position))
                    {
                        color = controller.Settings.GetHue(scene, position);
                        return true;
                    }
            }

            color = default;
            return false;
        }
    }
}
