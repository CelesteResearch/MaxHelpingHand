using System;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Game.Gameplay.Platforms.CrushBlocks;
using Celeste.Game.Execution.Processes;
using Celeste.Mods.MaxHelpingHand.Entities.Platforms.CrushBlocks;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Runtime.CrushBlocks
{
    public sealed class ReskinnableCrushBlockHooks : IDisposable
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, ReskinnableCrushBlockHooks> owners = new();
        private readonly ISourceEngineContext engine;
        private Hook activationHook;
        private bool disposed;

        public ReskinnableCrushBlockHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            owners.Add(engine, this);
            try
            {
                activationHook = new Hook(typeof(OriginalCrushBlockState).GetMethod("ActivateParticles",
                        BindingFlags.Instance | BindingFlags.NonPublic),
                    (Action<Action<OriginalCrushBlockState, Vector2>, OriginalCrushBlockState, Vector2>)
                    ActivateParticles);
            }
            catch
            {
                owners.Remove(engine);
                throw;
            }
        }

        internal static bool IsLoaded(ISourceEngineContext engine) => owners.TryGetValue(engine, out _);

        private void ActivateParticles(Action<OriginalCrushBlockState, Vector2> original, OriginalCrushBlockState self,
            Vector2 direction)
        {
            if (self is ReskinnableCrushBlock custom && ReferenceEquals(custom.SourceEngine, engine))
            {
                var particles = ((IOriginalGameplayProcess)engine).ParticleTypes.CrushBlock;
                var saved = particles.P_Activate;
                particles.P_Activate = custom.activateParticleColor;
                original(self, direction);
                particles.P_Activate = saved;
            }
            else original(self, direction);
        }

        public void Dispose()
        {
            if (disposed) return;
            activationHook.Dispose();
            owners.Remove(engine);
            disposed = true;
        }
    }
}
