using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Mods.MaxHelpingHand.Modules;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.MaxHelpingHand.Triggers.Camera
{
    [CustomEntity("MaxHelpingHand/CameraCatchupSpeedTrigger")]
    [SceneTracked]
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Triggers.CameraCatchupSpeedTrigger")]
    public class CameraCatchupSpeedTrigger : SourceTriggerEntityState
    {
        internal readonly float CatchupSpeed;
        private readonly bool revertOnLeave;
        private readonly MaxHelpingHandModule module;

        public CameraCatchupSpeedTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
                data.Position + offset, data.Width, data.Height,
                ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            CatchupSpeed = data.Float("catchupSpeed", 1f);
            revertOnLeave = data.Bool("revertOnLeave", true);
            module = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<MaxHelpingHandModule>().Single();
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            base.OnEnter(player);
            if (!revertOnLeave) module.Session.CameraCatchupSpeed = CatchupSpeed;
        }
    }
}
