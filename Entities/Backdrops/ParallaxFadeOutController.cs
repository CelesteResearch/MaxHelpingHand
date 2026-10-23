using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Presentation.Backdrops.Parallax;
using Celeste.Simulation.Presentation.Backdrops.Runtime;

namespace Celeste.Mods.MaxHelpingHand.Entities.Backdrops
{
    [CustomEntity("MaxHelpingHand/ParallaxFadeOutController")]
    [SceneTracked]
    public class ParallaxFadeOutController : SceneEntityState
    {
        private IDisposable backgroundPolicy;
        private IDisposable foregroundPolicy;

        public ParallaxFadeOutController(OriginalLevelSceneRoot level, OriginalLevelEntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)level.EngineContext).EntitySourceDataRegistration,
                data.Position + offset)
        {
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            var level = (OriginalLevelSceneRoot)scene;
            backgroundPolicy = level.Background.AddVisibilityPolicy(IsParallaxVisible);
            foregroundPolicy = level.Foreground.AddVisibilityPolicy(IsParallaxVisible);
        }

        public static bool IsParallaxVisible(BackdropPresentation backdrop, ISourceBackdropScene scene)
            => ((ISourceSceneTime)scene).TimeActive > 1f && backdrop is ParallaxBackdropPresentation parallax
                                                         && parallax.DoFadeIn && parallax.FadeIn > 0f;

        public override void Removed(object scene)
        {
            base.Removed(scene);
            ReleasePolicies();
        }

        public override void SceneEnd(object scene)
        {
            base.SceneEnd(scene);
            ReleasePolicies();
        }

        private void ReleasePolicies()
        {
            backgroundPolicy?.Dispose();
            foregroundPolicy?.Dispose();
            backgroundPolicy = foregroundPolicy = null;
        }

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
            => new ParallaxFadeOutController(level, data, offset);
    }
}
