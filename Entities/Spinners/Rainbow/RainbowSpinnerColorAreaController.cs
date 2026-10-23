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
    [CustomEntity("MaxHelpingHand/RainbowSpinnerColorAreaController = Load",
        "MaxHelpingHand/FlagRainbowSpinnerColorAreaController = Load")]
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias(
        "Celeste.Mod.MaxHelpingHand.Entities.RainbowSpinnerColorAreaController")]
    public class RainbowSpinnerColorAreaController : SceneEntityState
    {
        internal readonly RainbowColorProcess Process;
        private readonly RainbowGradientSettings normal;
        private readonly RainbowGradientSettings flagged;
        private readonly string flag;
        private bool flagLatestState;

        private bool IsFlagActive =>
            !string.IsNullOrEmpty(flag) && ((OriginalLevelSceneRoot)Scene).Session.GetFlag(flag);

        internal RainbowGradientSettings Settings => IsFlagActive ? flagged : normal;

        public static RainbowSpinnerColorAreaController Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
            => new(level.EngineContext, RainbowControllerFactory.GetOwner(level.EngineContext), data, offset);

        public RainbowSpinnerColorAreaController(ISourceEngineContext engine, MaxHelpingHandModule module,
            EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            Process = module.RainbowControllers;
            normal = new RainbowGradientSettings(data, false);
            flagged = new RainbowGradientSettings(data, true);
            flag = data.Attr("flag");
            Collider = new HitboxColliderGeometry(data.Width, data.Height);
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            flagLatestState = IsFlagActive;
            Process.AreaEnabled = true;
        }

        public override void Update()
        {
            base.Update();
            var state = IsFlagActive;
            if (state == flagLatestState) return;
            flagLatestState = state;
            Process.UpdateRainbowSpinners(Scene);
        }

        public override void Removed(object scene)
        {
            base.Removed(scene);
            if (((ISourceScene)scene).SourceScene.Entities.GetTrackedEntities(typeof(RainbowSpinnerColorAreaController))
                .Count <= 1)
                Process.AreaEnabled = false;
        }

        public override void SceneEnd(object scene)
        {
            base.SceneEnd(scene);
            Process.AreaEnabled = false;
        }
    }
}
