using System;
using System.Numerics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Tracking.Attributes;
using Celeste.Simulation.Gameplay.Platforms.StaticMovers;

namespace Celeste.Mods.MaxHelpingHand.Components.Platforms
{
    [EverestTrackedAs(typeof(SourceStaticMoverState))]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.StaticMoverWithLiftSpeed")]
    public class StaticMoverWithLiftSpeed : SourceStaticMoverState
    {
        public Action<Vector2> OnSetLiftSpeed;

        public override void MoveWithLiftSpeed(Vector2 amount, Vector2 liftSpeed)
        {
            OnSetLiftSpeed?.Invoke(liftSpeed);
            base.MoveWithLiftSpeed(amount, liftSpeed);
        }
    }
}
