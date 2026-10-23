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
    [CustomEntity("MaxHelpingHand/RainbowSpinnerColorControllerDisabler = Load")]
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias(
        "Celeste.Mod.MaxHelpingHand.Entities.RainbowSpinnerColorControllerDisabler")]
    public class RainbowSpinnerColorControllerDisabler : SceneEntityState
    {
        public static RainbowSpinnerColorControllerDisabler Load(OriginalLevelSceneRoot level,
            OriginalLevelRoomData room, Vector2 offset, OriginalLevelEntityData data)
            => new(level.EngineContext, RainbowControllerFactory.GetOwner(level.EngineContext), data, offset);

        public RainbowSpinnerColorControllerDisabler(ISourceEngineContext engine, MaxHelpingHandModule module,
            EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            module.Session.RainbowSpinnerCurrentColors = null;
        }
    }
}
