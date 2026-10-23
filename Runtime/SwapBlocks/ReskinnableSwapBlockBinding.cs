using System;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;

namespace Celeste.Mods.MaxHelpingHand.Runtime.SwapBlocks
{
    public sealed class ReskinnableSwapBlockBinding : IDisposable
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, ReskinnableSwapBlockBinding> owners = new();
        private readonly ISourceEngineContext engine;
        private bool disposed;

        public ReskinnableSwapBlockBinding(ISourceEngineContext engine)
        {
            this.engine = engine;
            owners.Add(engine, this);
        }

        internal static bool IsLoaded(ISourceEngineContext engine) => owners.TryGetValue(engine, out _);

        public void Dispose()
        {
            if (disposed) return;
            owners.Remove(engine);
            disposed = true;
        }
    }
}
