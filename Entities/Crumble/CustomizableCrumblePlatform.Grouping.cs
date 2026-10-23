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
    public partial class CustomizableCrumblePlatform
    {
        private CustomizableCrumblePlatform getOnePlatformWithPlayerOnTop()
        {
            return getBuddyCached(cache => cache.getOnePlatformWithPlayerOnTop, members =>
            {
                foreach (CustomizableCrumblePlatform platform in members)
                {
                    if (platform.GetPlayerOnTop() != null)
                    {
                        return platform;
                    }
                }

                return null;
            });
        }

        private CustomizableCrumblePlatform getOnePlatformWithPlayerClimbing()
        {
            return getBuddyCached(cache => cache.getOnePlatformWithPlayerClimbing, members =>
            {
                foreach (CustomizableCrumblePlatform platform in members)
                {
                    if (platform.GetPlayerClimbing() != null)
                    {
                        return platform;
                    }
                }

                return null;
            });
        }

        private bool IsActorOrSolidAtPlatform(bool solid) => SourceTrackedEntityCollision.Check(
            ((ISourceScene)Scene).SourceScene.Entities,
            solid ? "Celeste.Solid" : typeof(SourceActorEntityState).FullName, this);

        private IEnumerable<CustomizableCrumblePlatform> CollideGroupedPlatforms(Vector2 at)
        {
            foreach (CustomizableCrumblePlatform platform in ((ISourceScene)Scene).SourceScene.Entities
                     .GetTrackedEntities(typeof(CustomizableCrumblePlatform)))
                if (SourceEntityCollision.CheckAt(this, platform, at))
                    yield return platform;
        }

        private bool isGroupCollidingWithSomething()
        {
            return getBuddyCached(cache => cache.isGroupCollidingWithSomething, members =>
            {
                foreach (CustomizableCrumblePlatform platform in members)
                {
                    if (platform.IsActorOrSolidAtPlatform(false) || platform.IsActorOrSolidAtPlatform(true))
                    {
                        return true;
                    }
                }

                return false;
            });
        }

        private T getBuddyCached<T>(Func<CrumbleBuddyCache, CrumbleBuddyResult<T>> buddyCacheGetter,
            Func<List<CustomizableCrumblePlatform>, T> compute)
        {
            CrumbleBuddyResult<T> myResult = buddyCacheGetter(buddyCache);
            if (myResult.computed) return myResult.result;

            // compute and tell our buddies about it
            T result = compute(groupMembers);
            foreach (CustomizableCrumblePlatform platform in groupMembers)
            {
                CrumbleBuddyResult<T> buddyResult = buddyCacheGetter(platform.buddyCache);
                buddyResult.computed = true;
                buddyResult.result = result;
            }

            return result;
        }

        private IEnumerator<CustomizableCrumblePlatform> walkThroughCrumblePlatformTree()
        {
            HashSet<CustomizableCrumblePlatform> alreadyHandled = new HashSet<CustomizableCrumblePlatform>();
            Queue<CustomizableCrumblePlatform> toHandle = new Queue<CustomizableCrumblePlatform>();
            toHandle.Enqueue(this);

            while (toHandle.Count > 0)
            {
                CustomizableCrumblePlatform processing = toHandle.Dequeue();
                yield return processing;
                alreadyHandled.Add(processing);

                foreach (CustomizableCrumblePlatform platform in processing.linkedCrumblePlatforms)
                {
                    if (platform.Scene != null && !alreadyHandled.Contains(platform))
                    {
                        toHandle.Enqueue(platform);
                    }
                }
            }
        }
    }
}
