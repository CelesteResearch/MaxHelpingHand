using System;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.MaxHelpingHand.Runtime.Backdrops;

namespace Celeste.Mods.MaxHelpingHand.Runtime.Lava
{
    internal sealed class SidewaysLavaCamera : IDisposable
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, SidewaysLavaCamera> owners = new();
        private readonly ISourceEngineContext engine;
        private readonly MaxBackdropBufferState buffers;

        private SidewaysLavaCamera(ISourceEngineContext engine)
        {
            this.engine = engine;
            buffers = new MaxBackdropBufferState(engine);
        }

        internal static SidewaysLavaCamera For(ISourceEngineContext engine) =>
            owners.GetValue(engine, e => new SidewaysLavaCamera(e));

        private OriginalLevelSceneRoot Level => engine.Frame.Scenes.CurrentScene as OriginalLevelSceneRoot;
        internal int Width => !buffers.ZoomOutEnabled || Level == null ? 320 : Level.Camera.Viewport.Width;
        internal int Height => !buffers.ZoomOutEnabled || Level == null ? 180 : Level.Camera.Viewport.Height;

        public void Dispose()
        {
            buffers.Dispose();
            owners.Remove(engine);
        }
    }
}
