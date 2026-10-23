using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Gameplay.Platforms.Crumble;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest;
using Celeste.Everest.Diagnostics.Logging;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Controls.Source;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Platforms.Crumble;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Display.Colors;
using Celeste.Physics.Collision;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Gameplay.Actors.Foundation;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.MaxHelpingHand.Entities.Crumble
{
    [CustomEntity("MaxHelpingHand/CustomizableCrumblePlatform")]
    [SceneTracked]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.CustomizableCrumblePlatform")]
    public partial class CustomizableCrumblePlatform : EverestCrumblePlatformState
    {
        private enum FlagMode
        {
            None,
            UntilPlatformRespawn,
            UntilDeathOrRoomChange,
            Permanent
        }

        internal string outlineTexture;
        internal bool MatchesEngine(ISourceEngineContext engine) => ReferenceEquals(process, engine);

        private bool oneUse;
        private float respawnDelay;
        private float minCrumbleDurationOnTop;
        private float maxCrumbleDurationOnTop;
        private float crumbleDurationOnSide;
        private bool grouped;
        private bool onlyEmitSoundForPlayer;
        internal PackedColor fadeOutTint;
        private bool attachStaticMovers;
        private string flag;
        private bool setFlagOnPlayerContact;
        private FlagMode flagMode;
        private bool flagInverted;
        private float lightBlockingOpacity;

        // the former one contains the direct neighbors, the latter contains the entire group
        private HashSet<CustomizableCrumblePlatform>
            linkedCrumblePlatforms = new HashSet<CustomizableCrumblePlatform>();

        private List<CustomizableCrumblePlatform> groupMembers;

        // values cached and shared with group members to avoid doing the same collide checks multiple times on the same frame
        private readonly CrumbleBuddyCache buddyCache = new CrumbleBuddyCache();

        public static CustomizableCrumblePlatform Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
            => new CustomizableCrumblePlatform(data, offset, (IOriginalCrumbleProcess)level.EngineContext,
                level.SolidWorldFactory);

        public CustomizableCrumblePlatform(EntityData data, Vector2 offset, IOriginalCrumbleProcess process,
            ISourceSolidWorldFactory worlds) : base(data, offset, process, worlds)
        {
            OverrideTexture = data.Attr("texture", null);
            outlineTexture = data.Attr("outlineTexture", "objects/crumbleBlock/outline");
            oneUse = data.Bool("oneUse", false);
            respawnDelay = data.Float("respawnDelay", 2f);
            minCrumbleDurationOnTop = data.Float("minCrumbleDurationOnTop", 0.2f);
            maxCrumbleDurationOnTop = data.Float("maxCrumbleDurationOnTop", 0.6f);
            crumbleDurationOnSide = data.Float("crumbleDurationOnSide", 1f);
            grouped = data.Bool("grouped", false);
            onlyEmitSoundForPlayer = data.Bool("onlyEmitSoundForPlayer", false);
            string tint = data.Attr("fadeOutTint");
            fadeOutTint = tint.Length == 6 ? SourceHexColor.HexToColor(tint) : PackedColor.Gray;
            attachStaticMovers = data.Bool("attachStaticMovers", false);
            flag = data.Attr("flag");
            setFlagOnPlayerContact = data.Bool("setFlagOnPlayerContact", false);
            flagMode = data.Enum("flagMode", defaultValue: FlagMode.None);
            flagInverted = data.Bool("flagInverted");
            lightBlockingOpacity = data.Float("lightBlockingOpacity", 0.2f);
        }

        public override void Added(object scene)
        {
            base.Added(scene);

            foreach (SourceComponentState component in this)
            {
                if (component is SimulationCoroutine coroutine && coroutine != CrumbleOutlineFader &&
                    !CrumbleFalls.Contains(coroutine))
                {
                    // this coroutine is the sequence! hijack it
                    coroutine.RemoveSelf();
                    Add(new SimulationCoroutine((ISourceEngineContext)process, customSequence()));
                    break;
                }
            }

            if (oneUse)
            {
                // prevent the outline from appearing.
                CrumbleOutlineFader.RemoveSelf();
            }

            CrumbleOccluder.Alpha = lightBlockingOpacity;
        }

        private void addRange(HashSet<CustomizableCrumblePlatform> set,
            IEnumerable<CustomizableCrumblePlatform> elements)
        {
            foreach (CustomizableCrumblePlatform element in elements)
            {
                set.Add(element);
            }
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);

            if (grouped)
            {
                // get surrounding grouped crumble blocks: above, below, on left and on right.
                addRange(linkedCrumblePlatforms,
                    CollideGroupedPlatforms(Position + Vector2.UnitX).OfType<CustomizableCrumblePlatform>()
                        .Where(p => p.grouped));
                addRange(linkedCrumblePlatforms,
                    CollideGroupedPlatforms(Position - Vector2.UnitX).OfType<CustomizableCrumblePlatform>()
                        .Where(p => p.grouped));
                addRange(linkedCrumblePlatforms,
                    CollideGroupedPlatforms(Position + Vector2.UnitY).OfType<CustomizableCrumblePlatform>()
                        .Where(p => p.grouped));
                addRange(linkedCrumblePlatforms,
                    CollideGroupedPlatforms(Position - Vector2.UnitY).OfType<CustomizableCrumblePlatform>()
                        .Where(p => p.grouped));
            }
        }

        public override void Update()
        {
            if (groupMembers == null)
            {
                groupMembers = new List<CustomizableCrumblePlatform>();
                IEnumerator<CustomizableCrumblePlatform> crumblePlatformTreeWalker = walkThroughCrumblePlatformTree();
                while (crumblePlatformTreeWalker.MoveNext()) groupMembers.Add(crumblePlatformTreeWalker.Current);
                foreach (CustomizableCrumblePlatform groupMember in groupMembers)
                    groupMember.groupMembers = groupMembers;
                ((IEverestLoggerProcessEnvironment)process).Logger.Log(EverestLogLevel.Verbose,
                    "MaxHelpingHand/CustomizableCrumblePlatform",
                    $"Computed crumble platform group w/ {groupMembers.Count} members");
            }

            base.Update();

            // the buddy cache will be obsolete by the next frame
            buddyCache.getOnePlatformWithPlayerOnTop.Reset();
            buddyCache.getOnePlatformWithPlayerClimbing.Reset();
            buddyCache.isGroupCollidingWithSomething.Reset();
        }

        public override void Removed(object scene)
        {
            if (flagMode == FlagMode.UntilPlatformRespawn || flagMode == FlagMode.UntilDeathOrRoomChange)
            {
                ((OriginalLevelSceneRoot)Scene).Session.SetFlag(flag, flagInverted);
            }

            base.Removed(scene);
        }

        public override void SceneEnd(object scene)
        {
            if (flagMode == FlagMode.UntilPlatformRespawn || flagMode == FlagMode.UntilDeathOrRoomChange)
            {
                ((OriginalLevelSceneRoot)Scene).Session.SetFlag(flag, flagInverted);
            }

            base.SceneEnd(scene);
        }
    }
}
