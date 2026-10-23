using System;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Runtime;
using Celeste.Mods.MaxHelpingHand.Modules;

namespace Celeste.Mods.MaxHelpingHand.Runtime.Spinners.Rainbow
{
    internal static class RainbowControllerFactory
    {
        internal static MaxHelpingHandModule GetOwner(ISourceEngineContext engine)
        {
            foreach (var candidate in ((ISourceEverestModulesEnvironment)engine).Modules)
                if (candidate is MaxHelpingHandModule owner)
                    return owner;
            throw new InvalidOperationException("MaxHelpingHand is not loaded.");
        }
    }
}
