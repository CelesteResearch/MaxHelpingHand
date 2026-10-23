using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.MaxHelpingHand.Modules;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.MaxHelpingHand.Triggers.Rendering
{
    [CustomEntity("MaxHelpingHand/SetBloomStrengthTrigger")]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Triggers.SetBloomStrengthTrigger")]
    public class SetBloomStrengthTrigger : SourceTriggerEntityState
    {
        private readonly float value;
        private readonly MaxHelpingHandModule module;

        public SetBloomStrengthTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
                data.Position + offset, data.Width, data.Height,
                ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            value = data.Float("value", 0f);
            module = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<MaxHelpingHandModule>().Single();
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            (Scene as OriginalLevelSceneRoot).Bloom.Strength = value;
            module.Session.BloomStrength = value;
        }
    }
}
