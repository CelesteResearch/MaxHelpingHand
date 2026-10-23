using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Mods.MaxHelpingHand.Runtime.Barriers;
using Celeste.Scene.Entities;

namespace Celeste.Mods.MaxHelpingHand.Entities.Barriers
{
    [CustomEntity("MaxHelpingHand/SeekerBarrierColorControllerDisabler")]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.SeekerBarrierColorControllerDisabler")]
    public sealed class SeekerBarrierColorControllerDisabler : SceneEntityState
    {
        public SeekerBarrierColorControllerDisabler(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            SeekerBarrierProcess.For(engine).Module.Session.SeekerBarrierCurrentColors = null;
        }
    }
}
