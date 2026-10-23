using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Core.Maps;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Core.Reflection.Runtime;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Collectibles.Strawberries;
using Celeste.Graphics.Animation;
using Celeste.Mods.MaxHelpingHand.Runtime.Collectibles.Strawberries.NonPopping;
using Celeste.Simulation;

namespace Celeste.Mods.MaxHelpingHand.Entities.Collectibles.Strawberries.NonPopping
{
    [CustomEntity("MaxHelpingHand/NonPoppingStrawberry")]
    [RegisterStrawberry(true, false)]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.NonPoppingStrawberrySeed")]
    public class NonPoppingStrawberrySeed : OriginalStrawberrySeedState
    {
        private readonly IOriginalStrawberryProcess process;
        private readonly bool pulseEffect;

        public static OriginalStrawberryState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
        {
            var context = NonPoppingSeedConstruction.For(level.EngineContext);
            context.ReplaceSeeds = true;
            context.PulseEffect = EntityAttributeReader.Bool(data.Values, "pulseEffectOnSeeds", true);
            var strawberry = new OriginalStrawberryState(data, offset, new MapEntityId(room.Name, data.ID),
                (IOriginalStrawberryProcess)level.EngineContext);
            context.ReplaceSeeds = false;
            return strawberry;
        }

        public NonPoppingStrawberrySeed(OriginalStrawberryState strawberry, Vector2 position, int index,
            bool ghost, IOriginalStrawberryProcess process) : base(strawberry, position, index, ghost, process)
        {
            this.process = process;
            pulseEffect = NonPoppingSeedConstruction.For((ISourceEngineContext)process).PulseEffect;
        }

        internal bool MatchesEngine(ISourceEngineContext engine) => ReferenceEquals(process, engine);

        public override void Awake(object scene)
        {
            base.Awake(scene);
            if (!pulseEffect) Get<Sprite>().OnFrameChange = null;
        }
    }
}
