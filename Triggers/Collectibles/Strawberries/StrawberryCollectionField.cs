using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Content.Extensions.Collectibles;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Collectibles.Strawberries;
using Celeste.Scene.Following;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Collectibles.Strawberry;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.MaxHelpingHand.Triggers.Collectibles.Strawberries
{
    [CustomEntity("MaxHelpingHand/StrawberryCollectionField")]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Triggers.StrawberryCollectionField")]
    public class StrawberryCollectionField : SourceTriggerEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly List<ISourceStrawberryCollectible> followingBuffer = new();
        private bool bufferInUse;
        private float collectTimer = 0f;
        private bool delayBetweenBerries;
        private bool includeGoldens;

        public static StrawberryCollectionField Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(level.EngineContext, data, offset);

        public StrawberryCollectionField(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
                data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            this.engine = engine;
            delayBetweenBerries = EntityAttributeReader.Bool(data.Values, "delayBetweenBerries");
            includeGoldens = EntityAttributeReader.Bool(data.Values, "includeGoldens");
        }

        public override void OnStay(RoomPlayerSession player)
        {
            base.OnStay(player);
            if (collectTimer <= 0f)
            {
                var registry = ((ISourceInstalledStrawberryRegistryEnvironment)engine).StrawberryRegistry;
                var berryTypes = registry.GetBerryTypes();
                bool ownsBuffer = !bufferInUse;
                List<ISourceStrawberryCollectible> followingBerries = ownsBuffer
                    ? followingBuffer
                    : new List<ISourceStrawberryCollectible>();
                if (ownsBuffer) bufferInUse = true;
                try
                {
                    foreach (FollowerState follower in player.Leader.Followers)
                    {
                        if (berryTypes.Contains(follower.Entity.GetType()) &&
                            follower.Entity is ISourceStrawberryCollectible berry &&
                            (includeGoldens || !IsGolden(berry, registry))) followingBerries.Add(berry);
                    }

                    foreach (ISourceStrawberryCollectible berry in followingBerries)
                    {
                        berry.OnCollect();
                        if (delayBetweenBerries)
                        {
                            collectTimer = .3f;
                            break;
                        }
                    }
                }
                finally
                {
                    if (ownsBuffer)
                    {
                        followingBerries.Clear();
                        bufferInUse = false;
                    }
                }
            }
        }

        private static bool IsGolden(ISourceStrawberryCollectible berry, InstalledStrawberryRegistryState registry)
        {
            if (berry.GetType() == typeof(OriginalStrawberryState)) return ((OriginalStrawberryState)berry).Golden;
            var definitions = registry.GetRegisteredBerries();
            InstalledStrawberryRegistryState.RegisteredBerry definition = null;
            for (int i = 0; i < definitions.Count; i++)
            {
                if (definitions[i].berryClass == berry.GetType())
                {
                    definition = definitions[i];
                    break;
                }
            }

            return definition.blocksNormalCollection;
        }

        public override void Update()
        {
            base.Update();
            if (delayBetweenBerries) collectTimer -= engine.DeltaTime;
        }
    }
}
