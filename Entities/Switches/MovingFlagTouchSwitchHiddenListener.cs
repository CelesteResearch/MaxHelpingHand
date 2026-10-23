// SPDX-License-Identifier: MIT

using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Entities;

namespace Celeste.Mods.MaxHelpingHand.Entities.Switches
{
    internal sealed class MovingFlagTouchSwitchHiddenListener : SourceComponentState
    {
        private readonly ISourceEngineContext engine;
        private readonly string hideIfFlag;

        public MovingFlagTouchSwitchHiddenListener(ISourceEngineContext engine, string hideIfFlag) : base(true, false)
        {
            this.engine = engine;
            this.hideIfFlag = hideIfFlag;
        }

        public override void Update()
        {
            if (Entity != null)
            {
                var level = (OriginalLevelSceneRoot)Scene;
                if (level.Session.GetFlag(hideIfFlag))
                {
                    var entity = (SceneEntityState)Entity;
                    entity.Visible = entity.Active = entity.Collidable = false;
                    level.SourceScene.AddEntity(new ResurrectOnFlagDisableController(engine, entity, hideIfFlag));
                }
            }
        }
    }
}
