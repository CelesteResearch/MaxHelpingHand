// SPDX-License-Identifier: MIT

using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Entities;

namespace Celeste.Mods.MaxHelpingHand.Entities.Switches
{
    internal sealed class ResurrectOnFlagDisableController : SceneEntityState
    {
        private readonly SceneEntityState entity;
        private readonly string flag;

        public ResurrectOnFlagDisableController(ISourceEngineContext engine, SceneEntityState entity, string flag)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
            this.entity = entity;
            this.flag = flag;
        }

        public override void Update()
        {
            if (!((OriginalLevelSceneRoot)Scene).Session.GetFlag(flag))
            {
                entity.Visible = entity.Active = entity.Collidable = true;
                RemoveSelf();
            }
        }
    }
}
