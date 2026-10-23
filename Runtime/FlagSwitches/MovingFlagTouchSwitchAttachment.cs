// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using Celeste.Core.Components;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.MaxHelpingHand.Contracts;
using Celeste.Mods.MaxHelpingHand.Entities.Switches;
using Celeste.Game.Gameplay.Mechanisms.Switches;

namespace Celeste.Mods.MaxHelpingHand.Runtime.FlagSwitches
{
    internal sealed class MovingFlagTouchSwitchAttachment : SourceComponentState, IFlagMovingTouchSwitch
    {
        private readonly FlagSwitchGroups groups;
        private readonly OriginalTouchSwitchState moving;
        private readonly bool persistent;
        private readonly int id;
        private readonly string hideIfFlag;
        private List<FlagTouchSwitch> switches;
        private List<IFlagMovingTouchSwitch> movingSwitches;
        internal bool Registered { get; set; }
        public string Flag { get; }
        public bool Activated => moving.Switch.Activated;

        public bool Hidden => Registered && !string.IsNullOrEmpty(hideIfFlag) &&
                              ((OriginalLevelSceneRoot)moving.Scene).Session.GetFlag(hideIfFlag);

        public MovingFlagTouchSwitchAttachment(FlagSwitchGroups groups, OriginalTouchSwitchState moving, string flag,
            bool persistent, int id, string hideIfFlag) : base(true, false)
        {
            this.groups = groups;
            this.moving = moving;
            Flag = flag;
            this.persistent = persistent;
            this.id = id;
            this.hideIfFlag = hideIfFlag;
        }

        public override void EntityAwake()
        {
            base.EntityAwake();
            var level = (OriginalLevelSceneRoot)Scene;
            switches = new List<FlagTouchSwitch>();
            var tracked = level.SourceScene.Entities.GetTrackedEntities(typeof(FlagTouchSwitch));
            for (int i = 0; i < tracked.Count; i++)
                if (tracked[i] is FlagTouchSwitch touch && touch.Flag == Flag)
                    switches.Add(touch);
            movingSwitches = groups.CaptureMovingSwitches(level, Flag);
        }

        public override void Update() => RemoveSelf();
        public void Finish() => moving.Switch.Finish();

        public bool Complete() => groups.Complete((OriginalLevelSceneRoot)moving.Scene, Flag, false, persistent, id,
            switches, movingSwitches, Finished);

        private static void Finished()
        {
        }
    }
}
