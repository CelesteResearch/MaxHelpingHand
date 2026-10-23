using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;

namespace Celeste.Mods.MaxHelpingHand.Runtime.Collectibles.Strawberries.NonPopping
{
    internal sealed class NonPoppingSeedConstruction
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, NonPoppingSeedConstruction> Contexts = new();
        internal bool ReplaceSeeds;
        internal bool PulseEffect;

        internal static NonPoppingSeedConstruction For(ISourceEngineContext engine) =>
            Contexts.GetValue(engine, _ => new NonPoppingSeedConstruction());
    }
}
