// SPDX-License-Identifier: MIT

using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Processes;
using YamlDotNet.Serialization;

namespace Celeste.Mods.MaxHelpingHand.Sessions
{
    public partial class MaxHelpingHandSession
    {
        private ISourceEngineContext ambienceOwner;

        internal void BindAmbienceOwner(ISourceEngineContext engine) => ambienceOwner = engine;

        // Original compatibility property is a live read of the real chapter audio session.
        [YamlIgnore]
        public float AmbienceVolume => ambienceOwner == null
            ? 1f
            : ((IOriginalGameplayProcess)ambienceOwner).SaveContext.CurrentSave
            ?.CurrentSessionSafe?.Audio.AmbienceVolume ?? 1f;
    }
}
