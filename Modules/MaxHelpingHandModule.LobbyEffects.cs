using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Everest.Modules.Persistence;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.MaxHelpingHand.Presentation.Backdrops;
using Celeste.Mods.MaxHelpingHand.Sessions;
using Celeste.Mods.MaxHelpingHand.Triggers.Camera;
using Celeste.Physics.Collision;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Camera;
using Celeste.Simulation.Presentation.Backdrops.Rendering;
using Celeste.Simulation.Presentation.Backdrops.Runtime;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Modules
{
    public sealed partial class MaxHelpingHandModule
    {
        public override Type SessionType => typeof(MaxHelpingHandSession);
        public MaxHelpingHandSession Session => (MaxHelpingHandSession)_Session;
        private readonly Dictionary<OriginalLevelSceneRoot, StylegroundFadeState> stylegroundFades = new();

        private void LoadLobbyEffects()
        {
            using (new DetourConfigContext(new DetourConfig("MaxHelpingHand.CameraCatchup").WithPriority(int.MinValue))
                       .Use())
                hooks.Add(new Hook(typeof(RoomCamera).GetMethod(nameof(RoomCamera.GetFollowFactor)),
                    (Func<Func<RoomCamera, ISourceCameraPlayer, float>, RoomCamera, ISourceCameraPlayer, float>)
                    GetCameraFollowFactor));
            hooks.Add(new Hook(typeof(BackdropPresentation).GetMethod(nameof(BackdropPresentation.IsVisible)),
                (Func<Func<BackdropPresentation, ISourceBackdropScene, bool>, BackdropPresentation, ISourceBackdropScene
                    , bool>)IsBackdropVisible));
            hooks.Add(new Hook(
                typeof(BackdropRendererPresentation).GetMethod(nameof(BackdropRendererPresentation.Update)),
                (Action<Action<BackdropRendererPresentation, object>, BackdropRendererPresentation, object>)
                UpdateBackdropRenderer));
        }

        private void UnloadLobbyEffects()
        {
            foreach (var state in stylegroundFades.Values) state.Dispose();
            stylegroundFades.Clear();
        }

        private float GetCameraFollowFactor(Func<RoomCamera, ISourceCameraPlayer, float> original,
            RoomCamera camera, ISourceCameraPlayer player)
        {
            float value = original(camera, player);
            if (player is not RoomPlayerSession actual || actual.Scene is not OriginalLevelSceneRoot level ||
                !ReferenceEquals(level.EngineContext, SourceEngine)) return value;
            foreach (CameraCatchupSpeedTrigger trigger in ((ISourceScene)level).SourceScene.Entities
                     .GetTrackedEntities(typeof(CameraCatchupSpeedTrigger)))
                if (SourceEntityCollision.Check(actual, trigger))
                    return trigger.CatchupSpeed;
            return Session.CameraCatchupSpeed ?? value;
        }

        private bool IsBackdropVisible(Func<BackdropPresentation, ISourceBackdropScene, bool> original,
            BackdropPresentation backdrop, ISourceBackdropScene scene)
            => scene is OriginalLevelSceneRoot level && stylegroundFades.TryGetValue(level, out var fades) &&
               fades.ForceVisible(backdrop)
               || original(backdrop, scene);

        private void UpdateBackdropRenderer(Action<BackdropRendererPresentation, object> original,
            BackdropRendererPresentation renderer, object scene)
        {
            original(renderer, scene);
            if (scene is OriginalLevelSceneRoot level && ReferenceEquals(renderer, level.Background) &&
                stylegroundFades.TryGetValue(level, out var fades)) fades.Advance();
        }

        internal StylegroundFadeState GetStylegroundFades(OriginalLevelSceneRoot level)
        {
            if (!stylegroundFades.TryGetValue(level, out var fades))
                stylegroundFades.Add(level, fades = new StylegroundFadeState(level));
            return fades;
        }

        internal void ReleaseStylegroundFades(StylegroundFadeState fades)
        {
            if (!fades.Empty) return;
            fades.Dispose();
            stylegroundFades.Remove(fades.Level);
        }
    }
}
