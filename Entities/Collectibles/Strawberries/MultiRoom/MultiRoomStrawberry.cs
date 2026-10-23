using System.Linq;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Maps;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Collectibles.Strawberries;
using Celeste.Game.Gameplay.Collectibles.Strawberries.Seeds;
using Celeste.Mods.MaxHelpingHand.Modules;
using Celeste.Mods.MaxHelpingHand.Sessions.Collectibles;
using Celeste.Scene.Tracking;
using Celeste.Scene.Following;
using Celeste.Simulation;

namespace Celeste.Mods.MaxHelpingHand.Entities.Collectibles.Strawberries.MultiRoom
{
    [CustomEntity("MaxHelpingHand/MultiRoomStrawberry")]
    [RegisterStrawberry(true, false)]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.MultiRoomStrawberry")]
    public class MultiRoomStrawberry : OriginalStrawberryState
    {
        private readonly IOriginalStrawberryProcess process;
        private readonly MaxHelpingHandModule module;
        private readonly string playerRegistryKey;
        private List<OriginalStrawberrySeedState> matchingSeedBuffer = new();

        public static MultiRoomStrawberry Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new(data, offset, level.CreateEntityId(room, data),
            (IOriginalStrawberryProcess)level.EngineContext);

        private int seedCount;
        private MapEntityId id;

        public MultiRoomStrawberry(EntityData data, Vector2 offset, MapEntityId gid, IOriginalStrawberryProcess process)
            : base(data, offset, gid, process)
        {
            this.process = process;
            module = ((ISourceEverestModulesEnvironment)process).Modules.OfType<MaxHelpingHandModule>().Single();
            playerRegistryKey = ((ISourceSceneCatalogEnvironment)process).SceneCatalog.Find(typeof(RoomPlayerSession))
                .RegistryKey;
            seedCount = EntityAttributeReader.Int(data.Values, "seedCount");
            id = gid;
        }

        public override void Added(object scene)
        {
            // trick the berry into thinking it has vanilla seeds, so that it doesn't appear right away
            OriginalStrawberrySeedState dummySeed =
                new OriginalStrawberrySeedState(this, Vector2.Zero, 0, false, process);
            Seeds = new List<OriginalStrawberrySeedState> { dummySeed };

            base.Added(scene);

            ((ISourceScene)scene).SourceScene.RemoveEntity(dummySeed);
            Seeds = null;
        }

        public override void Update()
        {
            base.Update();

            if (WaitingOnSeeds)
            {
                RoomPlayerSession player =
                    (RoomPlayerSession)((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>(
                        playerRegistryKey);
                if (player != null)
                {
                    // look at all the player followers, and filter all the seeds that match our berry.
                    List<OriginalStrawberrySeedState> matchingSeeds = matchingSeedBuffer;
                    if (matchingSeeds == null)
                        matchingSeeds = matchingSeedBuffer = new List<OriginalStrawberrySeedState>();
                    matchingSeeds.Clear();
                    foreach (FollowerState follower in player.Leader.Followers)
                    {
                        if (follower.Entity is MultiRoomStrawberrySeed seed)
                        {
                            if (seed.BerryID.Level == id.Level && seed.BerryID.ID == id.ID)
                            {
                                matchingSeeds.Add(seed);
                            }
                        }
                    }

                    if (matchingSeeds.Count >= seedCount)
                    {
                        // all seeds have been gathered! associate the berry and the seeds, then trigger the cutscene.
                        matchingSeedBuffer = null;
                        Seeds = matchingSeeds;
                        foreach (OriginalStrawberrySeedState seed in matchingSeeds)
                        {
                            seed.Strawberry = this;
                        }

                        // build the "seed merging" cutscene with a transition listener to prevent it from breaking the game if the player transitions out before time is frozen.
                        StrawberrySeedsCutsceneState seedsCutscene = new StrawberrySeedsCutsceneState(this, process);
                        seedsCutscene.Add(new RoomTransitionListener()
                        {
                            OnOutBegin = () => ((OriginalLevelSceneRoot)Scene).CutscenePolicy.SkipCutscene()
                        });
                        ((ISourceScene)Scene).SourceScene.QueueSourceEntity(seedsCutscene);

                        // also clean up the session, since the seeds are now gone.
                        List<MultiRoomStrawberrySeedInfo> seedList = module.Session.CollectedMultiRoomStrawberrySeeds;
                        for (int i = 0; i < seedList.Count; i++)
                        {
                            if (seedList[i].BerryID.Level == id.Level && seedList[i].BerryID.ID == id.ID)
                            {
                                seedList.RemoveAt(i);
                                i--;
                            }
                        }
                    }
                }
            }
        }
    }
}
