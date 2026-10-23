// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.MaxHelpingHand.Entities.Glass;
using Celeste.Simulation.Gameplay.Player.Intro;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Modules
{
    public sealed partial class MaxHelpingHandModule
    {
        private void LoadGlass() => hooks.Add(new Hook(
            typeof(OriginalLevelSceneRoot).GetMethod(nameof(OriginalLevelSceneRoot.LoadLevel)),
            (Action<Action<OriginalLevelSceneRoot, PlayerIntroType, bool>, OriginalLevelSceneRoot, PlayerIntroType,
                bool>)
            RestoreGlassController));

        private void RestoreGlassController(Action<OriginalLevelSceneRoot, PlayerIntroType, bool> original,
            OriginalLevelSceneRoot level, PlayerIntroType intro, bool fromLoader)
        {
            if (ReferenceEquals(level.EngineContext, SourceEngine) &&
                Session.GlassBlockCurrentSettings is { } settings &&
                level.Session.LevelData is OriginalLevelRoomData room && !room.Entities.Any(entity =>
                    entity.Name is "MaxHelpingHand/CustomizableGlassBlockController"
                        or "MaxHelpingHand/CustomizableGlassBlockAreaController"))
            {
                var data = new EntityData
                {
                    Values = new Dictionary<string, object>
                    {
                        ["starColors"] = settings.StarColors, ["bgColor"] = settings.BgColor, ["wavy"] = settings.Wavy,
                        ["persistent"] = true
                    }
                };
                level.SourceScene.AddEntity(new CustomizableGlassBlockController(SourceEngine, data, Vector2.Zero));
            }

            original(level, intro, fromLoader);
        }
    }
}
