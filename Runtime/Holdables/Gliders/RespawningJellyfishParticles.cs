using System.Runtime.CompilerServices;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Processes;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.MaxHelpingHand.Runtime.Holdables.Gliders
{
    public sealed class RespawningJellyfishParticles
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, RespawningJellyfishParticles> Owners = new();
        public readonly ParticleTypePresentation NotGlow;

        private RespawningJellyfishParticles(ISourceEngineContext engine)
        {
            NotGlow = new ParticleTypePresentation(((IOriginalGameplayProcess)engine).ParticleTypes.Glider.P_Glow)
                { Color = PackedColor.Transparent, Color2 = PackedColor.Transparent };
        }

        public static RespawningJellyfishParticles For(ISourceEngineContext engine) => Owners.GetValue(engine, Create);
        private static RespawningJellyfishParticles Create(ISourceEngineContext engine) => new(engine);
        public static void Release(ISourceEngineContext engine) => Owners.Remove(engine);
    }
}
