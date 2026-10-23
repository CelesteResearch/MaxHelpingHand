using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.MaxHelpingHand.Modules;
using Celeste.Mods.MaxHelpingHand.Runtime.Spinners.Rainbow;
using Celeste.Mods.MaxHelpingHand.Sessions;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;

namespace Celeste.Mods.MaxHelpingHand.Entities.Spinners.Rainbow
{
    [SceneTracked]
    [CustomEntity("MaxHelpingHand/RainbowSpinnerColorController = Load",
        "MaxHelpingHand/FlagRainbowSpinnerColorController = Load")]
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias(
        "Celeste.Mod.MaxHelpingHand.Entities.RainbowSpinnerColorController")]
    public class RainbowSpinnerColorController : SceneEntityState
    {
        private readonly MaxHelpingHandModule module;
        internal readonly RainbowColorProcess Process;
        private readonly RainbowGradientSettings normal;
        private readonly RainbowGradientSettings flagged;
        private readonly string flag;
        private readonly RainbowSpinnerColorState sessionState;
        private bool flagLatestState;

        private bool IsFlagActive => !string.IsNullOrEmpty(flag) && Scene is OriginalLevelSceneRoot level &&
                                     level.Session.GetFlag(flag);

        internal RainbowGradientSettings Settings => IsFlagActive ? flagged : normal;

        public static RainbowSpinnerColorController Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
            => new(level.EngineContext, RainbowControllerFactory.GetOwner(level.EngineContext), data, offset);

        public RainbowSpinnerColorController(ISourceEngineContext engine, MaxHelpingHandModule module, EntityData data,
            Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.module = module;
            Process = module.RainbowControllers;
            normal = new RainbowGradientSettings(data, false);
            flagged = new RainbowGradientSettings(data, true);
            flag = data.Attr("flag");
            if (data.Bool("persistent")) sessionState = RainbowSpinnerColorState.FromData(data, normal, flagged);
            Add(new RoomTransitionListener
            {
                OnIn = progress => Process.TransitionProgress = progress,
                OnOut = progress => Process.TransitionProgress = progress,
                OnInBegin = () => Process.TransitionProgress = 0,
                OnInEnd = () => Process.TransitionProgress = -1
            });
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            ApplyToSession();
        }

        internal void ApplyToSession() => module.Session.RainbowSpinnerCurrentColors = sessionState;

        public override void Awake(object scene)
        {
            base.Awake(scene);
            flagLatestState = IsFlagActive;
            Process.Next = this;
            Process.Enabled = true;
        }

        public override void Removed(object scene)
        {
            base.Removed(scene);
            Process.Current = Process.Next;
            Process.Next = null;
            Process.TransitionProgress = -1;
            if (Process.Current == null) Process.Enabled = false;
        }

        public override void SceneEnd(object scene)
        {
            base.SceneEnd(scene);
            Process.ResetControllers(false);
        }

        public override void Update()
        {
            base.Update();
            Process.PromoteNext();
            var state = IsFlagActive;
            if (state == flagLatestState) return;
            flagLatestState = state;
            Process.UpdateRainbowSpinners(Scene);
        }
    }
}
