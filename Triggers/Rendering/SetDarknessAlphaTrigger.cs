using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.MaxHelpingHand.Triggers.Rendering
{
    [CustomEntity("MaxHelpingHand/SetDarknessAlphaTrigger")]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Triggers.SetDarknessAlphaTrigger")]
    public class SetDarknessAlphaTrigger : SourceTriggerEntityState
    {
        private readonly float value;

        public SetDarknessAlphaTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
                data.Position + offset, data.Width, data.Height,
                ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            value = data.Float("value", 0f);
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            var level = Scene as OriginalLevelSceneRoot;
            level.Lighting.Alpha = value;
            level.Session.LightingAlphaAdd = value - level.BaseLightingAlpha;
        }
    }
}
