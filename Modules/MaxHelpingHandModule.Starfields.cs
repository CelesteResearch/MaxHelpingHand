// SPDX-License-Identifier: MIT

using Celeste.Mods.MaxHelpingHand.Backdrops;

namespace Celeste.Mods.MaxHelpingHand.Modules
{
    public sealed partial class MaxHelpingHandModule
    {
        private void LoadStarfieldAuroras() => hooks.Add(new MaxStarfieldBackdropFactory(SourceEngine));
    }
}
