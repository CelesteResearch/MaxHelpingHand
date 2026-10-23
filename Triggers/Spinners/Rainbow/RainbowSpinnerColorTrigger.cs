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
    [CustomEntity("MaxHelpingHand/RainbowSpinnerColorTrigger")]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Triggers.RainbowSpinnerColorTrigger")]
    public class RainbowSpinnerColorTrigger : SourceTriggerEntityState
    {
        private readonly RainbowSpinnerColorController controller;

        public RainbowSpinnerColorTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
                data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            controller =
                new RainbowSpinnerColorController(engine, RainbowControllerFactory.GetOwner(engine), data, offset);
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            base.OnEnter(player);
            var scene = ((ISourceScene)Scene).SourceScene;
            var controllers = scene.Entities.GetTrackedEntities(typeof(RainbowSpinnerColorController));
            var entity = controllers.Count == 0 ? null : (RainbowSpinnerColorController)controllers[0];
            if (entity == controller) return;
            if (entity != null) scene.RemoveEntity(entity);
            scene.QueueSourceEntity(controller);
        }
    }
}
