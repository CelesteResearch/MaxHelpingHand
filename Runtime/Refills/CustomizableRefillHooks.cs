using System;
using System.Reflection;
using Celeste.Core.Scheduling;
using Celeste.Game.Gameplay.Collectibles.Refills;
using Celeste.Mods.MaxHelpingHand.Entities.Refills;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Runtime.Refills
{
    internal sealed class CustomizableRefillHooks : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly Hook updateY;

        public CustomizableRefillHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            updateY = new Hook(
                typeof(OriginalRefillState).GetMethod("UpdateY", BindingFlags.Instance | BindingFlags.NonPublic),
                (Action<Action<OriginalRefillState>, OriginalRefillState>)UpdateY);
        }

        private void UpdateY(Action<OriginalRefillState> original, OriginalRefillState self)
        {
            if (self is CustomizableRefill refill && ReferenceEquals(refill.SourceEngine, engine))
                refill.RotatedUpdateY();
            else
                original(self);
        }

        public void Dispose() => updateY.Dispose();
    }
}
