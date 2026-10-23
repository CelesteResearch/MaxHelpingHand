using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Tracking.Attributes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Entities.Triggers.Camera;
using Celeste.Mods.MaxHelpingHand.Components;

namespace Celeste.Mods.MaxHelpingHand.Triggers
{
    [CustomEntity("MaxHelpingHand/FlagToggleCameraOffsetTrigger")]
    public sealed class FlagToggleCameraOffsetTrigger : OriginalCameraOffsetTriggerState
    {
        public FlagToggleCameraOffsetTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(engine, data, offset)
        {
            Add(new FlagToggleComponent(data.Attr("flag"), data.Bool("inverted")));
        }
    }
}
