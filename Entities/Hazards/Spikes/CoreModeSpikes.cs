using System.Numerics;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Everest.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Environment.Core;
using Celeste.Simulation.Gameplay.Hazards.Spikes;

namespace Celeste.Mods.MaxHelpingHand.Entities.Hazards.Spikes
{
    [CustomEntity("MaxHelpingHand/CoreModeSpikesUp = LoadUp", "MaxHelpingHand/CoreModeSpikesDown = LoadDown",
        "MaxHelpingHand/CoreModeSpikesLeft = LoadLeft", "MaxHelpingHand/CoreModeSpikesRight = LoadRight")]
    [EverestTrackedAs(typeof(SpikesState))]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.CoreModeSpikes")]
    public class CoreModeSpikes : SpikesState
    {
        private readonly ISourceEngineContext engine;
        private string hotType;
        private string coldType;
        private int randomSeed;

        public static CoreModeSpikes LoadUp(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
        {
            data.Values["type"] = data.Attr("hotType", "default");
            return new CoreModeSpikes(level.EngineContext, data, offset, SpikeDirection.Up);
        }

        public static CoreModeSpikes LoadDown(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
        {
            data.Values["type"] = data.Attr("hotType", "default");
            return new CoreModeSpikes(level.EngineContext, data, offset, SpikeDirection.Down);
        }

        public static CoreModeSpikes LoadLeft(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
        {
            data.Values["type"] = data.Attr("hotType", "default");
            return new CoreModeSpikes(level.EngineContext, data, offset, SpikeDirection.Left);
        }

        public static CoreModeSpikes LoadRight(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
        {
            data.Values["type"] = data.Attr("hotType", "default");
            return new CoreModeSpikes(level.EngineContext, data, offset, SpikeDirection.Right);
        }

        public CoreModeSpikes(ISourceEngineContext engine, EntityData data, Vector2 offset, SpikeDirection direction)
            : base(data.Position + offset, (uint)direction > 1u ? data.Height : data.Width,
                direction, EntityAttributeReader.Attr(data.Values, "type", "default"), engine)
        {
            this.engine = engine;
            hotType = data.Attr("hotType", "default");
            coldType = data.Attr("coldType", "default");
            Add(new CoreModeListenerState(OnCoreModeChange));
            randomSeed = ((ISourceRandomEnvironment)engine).Random.Next();
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            SwapTexture(((OriginalLevelSceneRoot)Scene).Session.CoreMode);
        }

        private void OnCoreModeChange(RoomCoreMode mode) => SwapTexture(mode);

        private void SwapTexture(RoomCoreMode target)
        {
            string spikeType = target == RoomCoreMode.Cold ? coldType : hotType;
            string direction = Direction.ToString().ToLower();
            var spikeTextures = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas
                .GetAtlasSubtextures("danger/spikes/" + spikeType + "_" + direction);
            var random = ((ISourceRandomProcessEnvironment)engine).RandomProcess;
            random.PushRandom(randomSeed);
            foreach (object component in Components)
                if (component is SpriteImage image)
                    image.Texture = random.Random.Choose(spikeTextures);
            random.PopRandom();
        }
    }
}
