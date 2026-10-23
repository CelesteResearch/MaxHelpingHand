using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Maps;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Collectibles.Strawberries;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content.Atlas;
using Celeste.Mods.MaxHelpingHand.Modules;
using Celeste.Mods.MaxHelpingHand.Sessions.Collectibles;
using Celeste.Mods.MaxHelpingHand.Runtime.Collectibles.Strawberries.MultiRoom;
using Celeste.Scene.Entities;
using Celeste.Scene.Following;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Collision.Components;

namespace Celeste.Mods.MaxHelpingHand.Entities.Collectibles.Strawberries.MultiRoom
{
    [CustomEntity("MaxHelpingHand/MultiRoomStrawberrySeed")]
    [SceneTracked(false)]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.MultiRoomStrawberrySeed")]
    public class MultiRoomStrawberrySeed : OriginalStrawberrySeedState
    {
        private readonly IOriginalStrawberryProcess process;
        private readonly MaxHelpingHandModule module;

        private Sprite sprite
        {
            get => StrawberrySeedFields.GetSprite(this);
            set => StrawberrySeedFields.SetSprite(this, value);
        }

        private int index => StrawberrySeedFields.GetIndex(this);
        private bool ghost => StrawberrySeedFields.GetGhost(this);
        private FollowerState follower => StrawberrySeedFields.GetFollower(this);

        private Vector2 start
        {
            get => StrawberrySeedFields.GetStart(this);
            set => StrawberrySeedFields.SetStart(this, value);
        }

        private RoomPlayerSession player
        {
            get => StrawberrySeedFields.GetPlayer(this);
            set => StrawberrySeedFields.SetPlayer(this, value);
        }

        private float canLoseTimer
        {
            set => StrawberrySeedFields.SetCanLoseTimer(this, value);
        }

        internal string StartingRoom => startingRoom;
        internal bool MatchesEngine(ISourceEngineContext engine) => ReferenceEquals(process, engine);

        public static MultiRoomStrawberrySeed Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) =>
            new(data, offset, (IOriginalStrawberryProcess)level.EngineContext);

        private void RemoveCounters()
        {
            var list =
                ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(typeof(MultiRoomStrawberryCounter));
            foreach (SceneEntityState counter in list) counter.RemoveSelf();
        }

        public MapEntityId BerryID;
        private int seedCount;
        private bool displaySeedCount;

        private string startingRoom;

        private float canLoseTimerMirror;
        private bool spawnedAsFollower = false;

        private string spriteName;

        public MultiRoomStrawberrySeed(Vector2 position, int index, bool ghost, string sprite, string ghostSprite,
            IOriginalStrawberryProcess process) : base(null, position, index, ghost, process)
        {
            this.process = process;
            module = ((ISourceEverestModulesEnvironment)process).Modules.OfType<MaxHelpingHandModule>().Single();
            this.spriteName = ghost ? ghostSprite : sprite;

            foreach (SourceComponentState component in this)
            {
                if (component is PlayerColliderComponentState playerCollider)
                {
                    playerCollider.OnCollide = OnPlayerOverride;
                }

                if (component is FollowerState follower)
                {
                    follower.ParentEntityID = new MapEntityId("null", -1);
                }
            }
        }

        public MultiRoomStrawberrySeed(EntityData data, Vector2 offset, IOriginalStrawberryProcess process) : this(
            data.Position + offset, EntityAttributeReader.Int(data.Values, "index"),
            process.CheckStrawberry(new MapEntityId(EntityAttributeReader.Attr(data.Values, "berryLevel"),
                EntityAttributeReader.Int(data.Values, "berryID"))),
            EntityAttributeReader.Attr(data.Values, "sprite", "MaxHelpingHand/miniberry/miniberry"),
            EntityAttributeReader.Attr(data.Values, "ghostSprite", "MaxHelpingHand/miniberry/ghostminiberry"), process)
        {
            BerryID = new MapEntityId(EntityAttributeReader.Attr(data.Values, "berryLevel"),
                EntityAttributeReader.Int(data.Values, "berryID"));
            seedCount = EntityAttributeReader.Int(data.Values, "seedCount");
            displaySeedCount = EntityAttributeReader.Bool(data.Values, "displaySeedCount");
        }

        internal MultiRoomStrawberrySeed(RoomPlayerSession player, Vector2 position,
            MultiRoomStrawberrySeedInfo sessionSeedInfo, IOriginalStrawberryProcess process)
            : this(position, sessionSeedInfo.Index, process.CheckStrawberry(sessionSeedInfo.BerryID),
                sessionSeedInfo.Sprite, sessionSeedInfo.Sprite, process)
        {
            BerryID = sessionSeedInfo.BerryID;
            startingRoom = sessionSeedInfo.StartingRoom;

            // the seed is collected right away.
            this.player = player;
            spawnedAsFollower = true;

            // set the starting point so that the berry seed can go back there if it pops.
            start = sessionSeedInfo.StartingPoint;
        }

        public override void Added(object scene)
        {
            base.Added(scene);

            if (!spawnedAsFollower)
            {
                startingRoom = ((OriginalLevelSceneRoot)scene).Session.Level;

                if (((OriginalLevelSceneRoot)Scene).Session.GetFlag("collected_seeds_of_" + BerryID.ToString()))
                {
                    // if all seeds for this berry were already collected (the berry was already formed), commit remove self.
                    RemoveSelf();
                }
                else
                {
                    // if the seed already follows the player, commit remove self.
                    foreach (MultiRoomStrawberrySeedInfo sessionSeedInfo in module.Session
                                 .CollectedMultiRoomStrawberrySeeds)
                    {
                        if (sessionSeedInfo.Index == index && sessionSeedInfo.BerryID.ID == BerryID.ID &&
                            sessionSeedInfo.BerryID.Level == BerryID.Level)
                        {
                            RemoveSelf();
                            break;
                        }
                    }
                }
            }
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);

            if ((ghost && spriteName != "ghostberry/seed") || (!ghost && spriteName != "strawberry/seed"))
            {
                // the sprite is non-default. replace it.
                Sprite vanillaSprite = sprite;

                // build the new sprite.
                AtlasEntry frame0 = process.GameGraphics.GameAtlas["collectables/" + spriteName + "00"];
                AtlasEntry frame1 = process.GameGraphics.GameAtlas["collectables/" + spriteName + "01"];

                Sprite modSprite = new Sprite(process, process.GameGraphics.GameAtlas, spriteName);
                modSprite.CenterOrigin();
                modSprite.Justify = new Vector2(0.5f, 0.5f);
                modSprite.AddLoop("idle", 0.1f, new AtlasEntry[]
                {
                    frame0, frame0, frame0, frame0, frame0, frame0, frame0, frame0, frame0, frame0,
                    frame0, frame0, frame0, frame0, frame0, frame0, frame0, frame0, frame0, frame1
                });
                modSprite.AddLoop("noFlash", 0.1f, new AtlasEntry[] { frame0 });

                // copy over the values from the vanilla sprite
                modSprite.Position = vanillaSprite.Position;
                modSprite.Color = vanillaSprite.Color;
                modSprite.OnFrameChange = vanillaSprite.OnFrameChange;
                modSprite.Play("idle");
                modSprite.SetAnimationFrame(vanillaSprite.CurrentAnimationFrame);

                // and replace it for good
                OwnedComponents.Remove(vanillaSprite);
                OwnedComponents.Add(modSprite);
                sprite = modSprite;
            }

            if (spawnedAsFollower)
            {
                player.Leader.GainFollower(follower);
                canLoseTimerMirror = 0.25f;
                Collidable = false;
                Depth = -1000000;
                Tags |= OriginalSceneTags.Persistent;
            }
        }

        private void OnPlayerOverride(RoomPlayerSession player)
        {
            process.AudioSession.Play("event:/game/general/seed_touch", Position, process.SaveContext, "count", index);
            player.Leader.GainFollower(follower);
            canLoseTimerMirror = 0.25f;
            Collidable = false;
            Depth = -1000000;
            Tags |= OriginalSceneTags.Persistent;

            // Add the info for this berry seed to the session.
            MultiRoomStrawberrySeedInfo sessionSeedInfo = new MultiRoomStrawberrySeedInfo();
            sessionSeedInfo.Index = index;
            sessionSeedInfo.BerryID = BerryID;
            sessionSeedInfo.Sprite = spriteName;
            sessionSeedInfo.StartingRoom = startingRoom;
            sessionSeedInfo.StartingPoint = start;
            module.Session.CollectedMultiRoomStrawberrySeeds.Add(sessionSeedInfo);

            if (displaySeedCount)
            {
                // look at all the player followers, and filter all the seeds that match our berry.
                List<OriginalStrawberrySeedState> matchingSeeds = new List<OriginalStrawberrySeedState>();
                foreach (FollowerState follower in player.Leader.Followers)
                {
                    if (follower.Entity is MultiRoomStrawberrySeed seed)
                    {
                        if (seed.BerryID.Level == BerryID.Level && seed.BerryID.ID == BerryID.ID)
                        {
                            matchingSeeds.Add(seed);
                        }
                    }
                }

                // display a counter over Madeline's head
                RemoveCounters();
                ((ISourceScene)Scene).SourceScene.QueueSourceEntity(
                    new MultiRoomStrawberryCounter(matchingSeeds.Count + "/" + seedCount, process));
            }
        }

        public override void Update()
        {
            base.Update();

            // be sure the canLoseTimer always has a positive value. we don't want the player to lose this berry seed.
            canLoseTimerMirror -= process.DeltaTime;
            if (canLoseTimerMirror < 1f)
            {
                canLoseTimerMirror = 1000f;
                canLoseTimer = 1000f;
            }
        }
    }
}
