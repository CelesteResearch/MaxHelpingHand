using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Runtime;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.MaxHelpingHand.Triggers;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Intro;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Modules
{
    public sealed partial class MaxHelpingHandModule : SourceEverestModule
    {
        public override Type SettingsType => typeof(Celeste.Mods.MaxHelpingHand.Settings.MaxHelpingHandSettings);

        public Celeste.Mods.MaxHelpingHand.Settings.MaxHelpingHandSettings Settings =>
            (Celeste.Mods.MaxHelpingHand.Settings.MaxHelpingHandSettings)_Settings;

        internal OriginalLevelSceneRoot LoadingLevel { get; private set; }
        internal bool IsRespawning { get; private set; }

        private readonly List<IDisposable> hooks = new List<IDisposable>();

        public MaxHelpingHandModule(ISourceEngineContext engine) : base(engine)
        {
        }

        public override void Load()
        {
            hooks.Add(((global::Celeste.Core.Reflection.Interop.ISourceModInteropEnvironment)SourceEngine).ModInterop
                .RegisterOwned(new global::Celeste.Core.Reflection.Interop.SourceModInteropActualTypeBinding(
                    typeof(global::Celeste.Mods.MaxHelpingHand.Interop.GravityHelperImports.Interop),
                    global::Celeste.Mods.MaxHelpingHand.Interop.GravityHelperImports.For(SourceEngine).Imports)));
            hooks.Add(((global::Celeste.Core.Reflection.Interop.ISourceModInteropEnvironment)SourceEngine).ModInterop
                .RegisterOwned(new global::Celeste.Core.Reflection.Interop.SourceModInteropActualTypeBinding(
                    typeof(EntityNameRegistry), null)));
            LoadFlagSwitches();
            LoadMovingFlagTouchSwitches();
            LoadGlass();
            LoadVisualFeatures();
            hooks.Add(new global::Celeste.Mods.MaxHelpingHand.Runtime.Backdrops.CustomStarsRegistration(SourceEngine));
            LoadSnowCustomColors();
            LoadSilhouette();
            LoadMusicTriggers();
            LoadAnimatedParallaxes();
            LoadStarfieldAuroras();
            LoadLobbyEffects();
            hooks.Add(new global::Celeste.Mods.MaxHelpingHand.Runtime.CrumblePlatformHooks(SourceEngine));
            hooks.Add(global::Celeste.Mods.MaxHelpingHand.Runtime.Flags.Spawn.SpawnFlagHooks.For(SourceEngine));
            hooks.Add(global::Celeste.Mods.MaxHelpingHand.Runtime.Backdrops.ParallaxFadeSpeedHooks.For(SourceEngine));
            hooks.Add(instantLavaBlockerHooks =
                global::Celeste.Mods.MaxHelpingHand.Runtime.Lava.InstantLavaBlockerHooks.For(SourceEngine));
            LoadUpsideDownTracking();
            hooks.Add(new global::Celeste.Mods.MaxHelpingHand.Collision.SidewaysCollisionHooks(SourceEngine));
            LoadStaticPuffers();
            LoadRainbowControllers();
            hooks.Add(global::Celeste.Mods.MaxHelpingHand.Runtime.Tutorials.FancyTextTutorialHooks.For(SourceEngine));
            hooks.Add(global::Celeste.Mods.MaxHelpingHand.Runtime.Lava.SidewaysLavaCamera.For(SourceEngine));
            LoadKevinBarriers();
            hooks.Add(
                new global::Celeste.Mods.MaxHelpingHand.Runtime.Hazards.Bumpers.BumperNotCoreModeHooks(SourceEngine));
            hooks.Add(
                new global::Celeste.Mods.MaxHelpingHand.Runtime.Collectibles.Strawberries.MultiRoom.MultiRoomSeedHooks(
                    SourceEngine, this));
            LoadSeekerBarriers();
            LoadCustomizableRefills();
            LoadRespawningJellyfish();
            LoadNoDashRefillSprings();
            hooks.Add(
                new global::Celeste.Mods.MaxHelpingHand.Runtime.Collectibles.Strawberries.NonPopping.
                    NonPoppingSeedHooks(SourceEngine));
            hooks.Add(new global::Celeste.Mods.MaxHelpingHand.Runtime.Switches.TimedTouchSwitchTimerHooks(
                SourceEngine));
            LoadRenderSettings();
            hooks.Add(new global::Celeste.Mods.MaxHelpingHand.Runtime.Physics.Ice.IcePhysicsHooks(SourceEngine, this));
            hooks.Add(
                new global::Celeste.Mods.MaxHelpingHand.Runtime.Scenery.Billboards.ReskinnableBillboardHooks(
                    SourceEngine));
            hooks.Add(
                new global::Celeste.Mods.MaxHelpingHand.Runtime.Collectibles.Hearts.ReskinnableCrystalHeartHooks(
                    SourceEngine));
            hooks.Add(new global::Celeste.Mods.MaxHelpingHand.Runtime.Camera.OneWayCameraHooks(SourceEngine));
            hooks.Add(
                new global::Celeste.Mods.MaxHelpingHand.Runtime.CrushBlocks.ReskinnableCrushBlockHooks(SourceEngine));
            hooks.Add(
                new global::Celeste.Mods.MaxHelpingHand.Runtime.SwapBlocks.ReskinnableSwapBlockBinding(SourceEngine));
            hooks.Add(new Hook(typeof(OriginalLevelSceneRoot).GetMethod(nameof(OriginalLevelSceneRoot.LoadLevel)),
                (Action<Action<OriginalLevelSceneRoot, PlayerIntroType, bool>, OriginalLevelSceneRoot, PlayerIntroType,
                    bool>)OnLoadLevel));
            hooks.Add(new Hook(typeof(RoomPlayerSession).GetProperty(nameof(RoomPlayerSession.CameraTarget)).GetMethod,
                (Func<Func<RoomPlayerSession, Vector2>, RoomPlayerSession, Vector2>)CameraOffsetBorder
                    .ModifyCameraTarget));
            hooks.Add(new Hook(typeof(OriginalLevelSceneRoot).GetMethod(nameof(OriginalLevelSceneRoot.TransitionTo)),
                (Action<Action<OriginalLevelSceneRoot, OriginalLevelRoomData, Vector2>, OriginalLevelSceneRoot,
                    OriginalLevelRoomData, Vector2>)OnTransition));
        }

        public override void Unload()
        {
            UnloadRainbowControllers();
            UnloadUpsideDownTracking();
            UnloadVisualFeatures();
            UnloadSnowCustomColors();
            UnloadLobbyEffects();
            UnloadCollisionRegistrations();
            for (int index = hooks.Count - 1; index >= 0; index--) hooks[index].Dispose();
            hooks.Clear();
            FlagSwitchGroups?.ClearDreamGateBindings();
        }

        private void OnLoadLevel(Action<OriginalLevelSceneRoot, PlayerIntroType, bool> original,
            OriginalLevelSceneRoot level, PlayerIntroType intro, bool fromLoader)
        {
            var priorLevel = LoadingLevel;
            bool priorRespawning = IsRespawning;
            LoadingLevel = level;
            IsRespawning = intro != PlayerIntroType.Transition;
            try
            {
                original(level, intro, fromLoader);
            }
            finally
            {
                LoadingLevel = priorLevel;
                IsRespawning = priorRespawning;
            }
        }

        private static void OnTransition(Action<OriginalLevelSceneRoot, OriginalLevelRoomData, Vector2> original,
            OriginalLevelSceneRoot level, OriginalLevelRoomData next, Vector2 direction)
        {
            foreach (CameraOffsetBorder border in ((ISourceScene)level).SourceScene.Entities.GetTrackedEntities(
                         typeof(CameraOffsetBorder)))
                border.Active = border.Collidable = false;
            original(level, next, direction);
        }
    }
}
