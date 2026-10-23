// SPDX-License-Identifier: MIT

using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Areas;
using Celeste.Game.Chapters.Flow;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Session;
using Celeste.Game.Gameplay.Mechanisms.Switches;
using Celeste.Scene.Entities;

namespace Celeste.Mods.MaxHelpingHand.Entities.Switches
{
    internal sealed class MovingFlagTouchSwitchErrorSpawner : SceneEntityState
    {
        private readonly ISourceEngineContext engine;

        public MovingFlagTouchSwitchErrorSpawner(ISourceEngineContext engine) : base(
            ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
            this.engine = engine;
        }

        public override void Update()
        {
            var process = (IOriginalTouchSwitchProcess)engine;
            process.AudioSession.SetMusic(null);
            ((IOriginalLevelEnterStaticEnvironment)engine).LevelEnterStatics.ErrorMessage =
                "{big}Oops!{/big}{n}To use {# F94A4A}Moving Flag Touch Switches{#}, you need to have {# d678db}Outback Helper{#} installed!";
            var session = ((OriginalLevelSceneRoot)Scene).Session;
            var area = new OriginalAreaKey
                { ID = session.AreaID, Mode = session.Mode, SID = session.AreaSID, SIDID = session.AreaSIDID };
            var enter = (IOriginalLevelEnterProcess)engine;
            enter.LevelEnterPolicy.Go(((IOriginalSessionEnvironment)engine).Sessions.CreateSession(area), false, enter);
        }
    }
}
