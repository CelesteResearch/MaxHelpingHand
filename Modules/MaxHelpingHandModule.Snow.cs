using System;
using Celeste.Everest.Runtime.Levels.Update;
using Celeste.Formats.Map.Binary.Source;
using Celeste.Game.Content.Maps;
using Celeste.Mods.MaxHelpingHand.Backdrops.Weather.Snow;
using Celeste.Simulation.Presentation.Backdrops.Runtime;

namespace Celeste.Mods.MaxHelpingHand.Modules
{
    public sealed partial class MaxHelpingHandModule
    {
        // Optional camera integrations update this engine-owned state at their level-start boundary.
        internal bool SnowTileToViewport { get; set; }

        private void LoadSnowCustomColors()
            => ((IEverestLevelEntityLoadEnvironment)SourceEngine).LevelUpdateEvents.OnLoadBackdrop += LoadSnowBackdrop;

        private void UnloadSnowCustomColors()
            => ((IEverestLevelEntityLoadEnvironment)SourceEngine).LevelUpdateEvents.OnLoadBackdrop -= LoadSnowBackdrop;

        private BackdropPresentation LoadSnowBackdrop(OriginalChapterMapData map, BinaryPackerElement child,
            BinaryPackerElement above)
            => child.Name.Equals("MaxHelpingHand/SnowCustomColors", StringComparison.OrdinalIgnoreCase)
                ? SnowCustomColors.Create(SourceEngine, child, this)
                : null;
    }
}
