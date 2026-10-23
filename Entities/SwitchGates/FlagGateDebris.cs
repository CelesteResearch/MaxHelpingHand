using Celeste.Core.Scheduling;
using Celeste.Simulation.Gameplay.Actors.Debris;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.MaxHelpingHand.Entities.SwitchGates
{
    internal sealed class FlagGateDebris : DebrisState
    {
        public FlagGateDebris(ISourceEngineContext engine, IPlatformTargetMotionPolicy targetMotion)
            : base(engine, targetMotion)
        {
        }
    }
}
