using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Mods.MaxHelpingHand.Entities.Spinners.Rainbow;
using Celeste.Mods.MaxHelpingHand.Runtime.Spinners.Rainbow;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.MaxHelpingHand.Triggers.Spinners.Rainbow
{
    [CustomEntity("MaxHelpingHand/RainbowSpinnerColorFadeTrigger")]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Triggers.RainbowSpinnerColorFadeTrigger")]
    public class RainbowSpinnerColorFadeTrigger : SourceTriggerEntityState
    {
        private readonly RainbowSpinnerColorController controllerA;
        private readonly RainbowSpinnerColorController controllerB;
        private readonly PositionModes positionMode;
        private readonly RainbowColorProcess process;
        private bool diedInTrigger;

        public RainbowSpinnerColorFadeTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
                data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            var owner = RainbowControllerFactory.GetOwner(engine);
            controllerA = new RainbowSpinnerColorController(engine, owner, EndData(data, "A"), Vector2.Zero);
            controllerB = new RainbowSpinnerColorController(engine, owner, EndData(data, "B"), Vector2.Zero);
            process = owner.RainbowControllers;
            positionMode = data.Enum("direction", PositionModes.NoEffect);
        }

        private static EntityData EndData(EntityData data, string suffix) => new()
        {
            Values = new Dictionary<string, object>
            {
                ["colors"] = data.Attr("colors" + suffix, ""),
                ["gradientSize"] = data.Float("gradientSize" + suffix, 0),
                ["loopColors"] = data.Bool("loopColors" + suffix, false),
                ["centerX"] = data.Float("centerX" + suffix, 0),
                ["centerY"] = data.Float("centerY" + suffix, 0),
                ["gradientSpeed"] = data.Float("gradientSpeed" + suffix, 0),
                ["persistent"] = data.Bool("persistent", false)
            }
        };

        public override void OnEnter(RoomPlayerSession player)
        {
            base.OnEnter(player);
            var scene = ((ISourceScene)Scene).SourceScene;
            if (scene.Entities.GetTrackedEntities(typeof(RainbowSpinnerColorController)).Count == 0)
                scene.QueueSourceEntity(controllerA);
        }

        public override void OnStay(RoomPlayerSession player)
        {
            base.OnStay(player);
            process.Current = controllerA;
            process.Next = controllerB;
            process.TransitionProgress = GetPositionLerp(player, positionMode);
        }

        public override void OnLeave(RoomPlayerSession player)
        {
            base.OnLeave(player);
            if (player.Dead)
            {
                diedInTrigger = true;
                return;
            }

            var chosen = process.Next = process.TransitionProgress > .5f ? controllerB : controllerA;
            var scene = ((ISourceScene)Scene).SourceScene;
            var controllers = scene.Entities.GetTrackedEntities(typeof(RainbowSpinnerColorController));
            var entity = controllers.Count == 0 ? null : (RainbowSpinnerColorController)controllers[0];
            if (entity != chosen)
            {
                scene.RemoveEntity(entity);
                scene.QueueSourceEntity(chosen);
            }

            process.Current = null;
            process.TransitionProgress = -1f;
        }

        public override void Removed(object scene)
        {
            base.Removed(scene);
            if (!diedInTrigger) return;
            process.Current = null;
            process.Next = null;
            process.TransitionProgress = -1f;
        }

        public override void SceneEnd(object scene)
        {
            base.SceneEnd(scene);
            if (!PlayerIsInside) return;
            if (process.TransitionProgress > .5f) controllerB.ApplyToSession();
            else controllerA.ApplyToSession();
            process.TransitionProgress = -1f;
        }
    }
}
