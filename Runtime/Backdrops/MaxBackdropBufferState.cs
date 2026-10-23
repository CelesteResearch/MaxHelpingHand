using System;
using System.Reflection;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Runtime;
using Celeste.Everest.Modules.Updating;
using Celeste.Everest.Reflection.Runtime;
using Celeste.Game.Execution.LevelLoader;
using Celeste.Game.Presentation.Effects.Bloom;
using Celeste.Simulation.Runtime.Session;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Runtime.Backdrops
{
    public sealed class MaxBackdropBufferState : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly Hook startLevelHook;
        private bool extendedCamera;
        private bool prototypeCamera;
        public bool ZoomOutEnabled => extendedCamera || prototypeCamera;

        public int Width =>
            !ZoomOutEnabled ? 320 : ((IOriginalBloomProcess)engine).Graphics.Buffers.Gameplay?.Width ?? 320;

        public int Height =>
            !ZoomOutEnabled ? 180 : ((IOriginalBloomProcess)engine).Graphics.Buffers.Gameplay?.Height ?? 180;

        public MaxBackdropBufferState(ISourceEngineContext engine)
        {
            this.engine = engine;
            startLevelHook = new Hook(
                typeof(OriginalLevelLoaderScene).GetMethod("StartLevel",
                    BindingFlags.Instance | BindingFlags.NonPublic),
                (Action<Action<OriginalLevelLoaderScene>, OriginalLevelLoaderScene>)StartLevel);
        }

        private void StartLevel(Action<OriginalLevelLoaderScene> original, OriginalLevelLoaderScene loader)
        {
            if (ReferenceEquals(loader.EngineContext, engine)) UpdateCapabilities(loader.Level.Session);
            original(loader);
        }

        public void UpdateCapabilities(ChapterCompletionSession session)
        {
            var loader = ((IEverestLoaderProcessEnvironment)engine).Loader;
            extendedCamera = false;
            if (loader.DependencyLoaded(new EverestModuleMetadata
                    { Name = "ExtendedCameraDynamics", Version = new Version(1, 0, 3) }))
            {
                var type =
                    ((ISourceRuntimeTypeDiscoveryEnvironment)engine).RuntimeTypeDiscovery.ResolveRuntimeTypeAlias(
                        "Celeste.Mod.ExCameraDynamics.Code.Module.ExCameraAreaMetadata");
                var metadata = type.GetMethod("TryGetCameraMetadata", BindingFlags.Public | BindingFlags.Static)
                    .Invoke(null, new object[] { session });
                extendedCamera = metadata != null && (bool)type.GetProperty("EnableExtendedCamera").GetValue(metadata);
            }

            prototypeCamera = false;
            if (loader.TryGetDependency(
                    new EverestModuleMetadata { Name = "ZoomOutHelperPrototype", Version = new Version(0, 1, 1) },
                    out var module))
                prototypeCamera = (bool)module.GetType()
                    .GetMethod("SessionHasZoomOut", BindingFlags.Static | BindingFlags.NonPublic)
                    .Invoke(null, new object[] { session, null, null });
        }

        public void Dispose() => startLevelHook.Dispose();
    }
}
