using System;
using Celeste.Everest.Runtime.Levels.Update;
using Celeste.Formats.Map.Binary.Source;
using Celeste.Game.Content.Maps;
using Celeste.Game.Presentation.PostProcessing.ColorGrading;
using Celeste.Mods.MaxHelpingHand.Backdrops;
using Celeste.Mods.MaxHelpingHand.Triggers;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Presentation.Backdrops.Runtime;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Modules
{
    public sealed partial class MaxHelpingHandModule
    {
        private void LoadVisualFeatures()
        {
            hooks.Add(new Hook(
                typeof(OriginalLevelColorGradeState).GetMethod(nameof(OriginalLevelColorGradeState.Update)),
                (Action<Action<OriginalLevelColorGradeState, float>, OriginalLevelColorGradeState, float>)
                UpdateColorGrade));
            ((IEverestLevelEntityLoadEnvironment)SourceEngine).LevelUpdateEvents.OnLoadBackdrop += LoadVisualBackdrop;
        }

        private void UnloadVisualFeatures()
            => ((IEverestLevelEntityLoadEnvironment)SourceEngine).LevelUpdateEvents.OnLoadBackdrop -=
                LoadVisualBackdrop;

        private void UpdateColorGrade(Action<OriginalLevelColorGradeState, float> original,
            OriginalLevelColorGradeState state, float deltaTime)
        {
            if (ReferenceEquals(state.Level.EngineContext, SourceEngine))
                foreach (ColorGradeFadeTrigger trigger in ((ISourceScene)state.Level).SourceScene.Entities
                         .GetTrackedEntities(typeof(ColorGradeFadeTrigger)))
                    if (trigger.Triggered)
                        return;
            original(state, deltaTime);
        }

        private BackdropPresentation LoadVisualBackdrop(OriginalChapterMapData map, BinaryPackerElement child,
            BinaryPackerElement above)
        {
            if (child.Name.Equals("MaxHelpingHand/HeatWaveNoColorGrade", StringComparison.OrdinalIgnoreCase))
                return new HeatWaveNoColorGrade(SourceEngine, child.AttrBool("controlColorGradeWhenActive"),
                    child.AttrBool("renderParticles", true));
            if (child.Name.Equals("MaxHelpingHand/BlackholeCustomColors", StringComparison.OrdinalIgnoreCase))
                return Backdrops.Blackholes.BlackholeCustomColors.Create(SourceEngine, child);
            return null;
        }
    }
}
