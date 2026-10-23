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
        private IEnumerator customSequence()
        {
            while (true)
            {
                // wait until player is on top
                CustomizableCrumblePlatform triggeredPlatform = getOnePlatformWithPlayerOnTop();
                bool onTop;
                if (triggeredPlatform != null)
                {
                    onTop = true;
                    process.InputSource.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                }
                else
                {
                    triggeredPlatform = getOnePlatformWithPlayerClimbing();
                    if (triggeredPlatform == null)
                    {
                        yield return null;
                        continue;
                    }

                    onTop = false;
                    process.InputSource.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
                }

                // only emit sound if we should emit sound no matter what, or if the current platform is the one that got triggered.
                if (!onlyEmitSoundForPlayer || triggeredPlatform == this)
                {
                    process.AudioSession.Play("event:/game/general/platform_disintegrate", Center, process.SaveContext);
                }

                // the player touched the platform!
                if (flagMode != FlagMode.None && setFlagOnPlayerContact)
                {
                    ((OriginalLevelSceneRoot)Scene).Session.SetFlag(flag, !flagInverted);
                }

                // make pieces shake and emit particles
                CrumbleShaker.ShakeFor(onTop ? maxCrumbleDurationOnTop : crumbleDurationOnSide, removeOnFinish: false);
                foreach (SpriteImage image in CrumbleImages)
                {
                    ((OriginalLevelSceneRoot)Scene).Particles.Emit(process.ParticleTypes.CrumblePlatform.P_Crumble, 2,
                        Position + image.Position + new Vector2(0f, 2f), Vector2.One * 3f);
                }

                float targetTime = (onTop ? minCrumbleDurationOnTop : maxCrumbleDurationOnTop);
                for (float time = 0f; time < targetTime; time += 0.2f)
                {
                    yield return Math.Min(0.2f, targetTime - time);
                    foreach (SpriteImage image in CrumbleImages)
                    {
                        ((OriginalLevelSceneRoot)Scene).Particles.Emit(process.ParticleTypes.CrumblePlatform.P_Crumble,
                            2, Position + image.Position + new Vector2(0f, 2f), Vector2.One * 3f);
                    }
                }

                // wait for a bit more
                float timer = (onTop
                    ? maxCrumbleDurationOnTop - minCrumbleDurationOnTop
                    : crumbleDurationOnSide - maxCrumbleDurationOnTop);
                if (onTop)
                {
                    while (timer > 0f && getOnePlatformWithPlayerOnTop() != null)
                    {
                        yield return null;
                        timer -= process.DeltaTime;
                    }
                }
                else
                {
                    while (timer > 0f)
                    {
                        yield return null;
                        timer -= process.DeltaTime;
                    }
                }

                // make the platform disappear
                CrumbleOutlineFader.Replace(RunCrumbleOutlineFade(1f));
                CrumbleOccluder.Visible = false;
                Collidable = false;
                if (attachStaticMovers)
                {
                    DisableStaticMovers();
                }

                if (flagMode != FlagMode.None && !setFlagOnPlayerContact)
                {
                    ((OriginalLevelSceneRoot)Scene).Session.SetFlag(flag, !flagInverted);
                }

                float delay = 0.05f;
                for (int m = 0; m < 4; m++)
                {
                    for (int i = 0; i < CrumbleImages.Count; i++)
                    {
                        if (i % 4 - m == 0)
                        {
                            CrumbleFalls[i].Replace(RunCrumbleTileOut(CrumbleImages[CrumbleFallOrder[i]], delay * m));
                        }
                    }
                }

                if (oneUse)
                {
                    // wait for the platform to animate for a bit (1 second) then delete it.
                    yield return 1f;
                    RemoveSelf();
                    ((ISourceScene)Scene).SourceScene.OnEndOfFrame += () =>
                    {
                        if (groupMembers == null) return; // seems like another member already took care of it.
                        List<CustomizableCrumblePlatform> platforms = groupMembers;
                        ((IEverestLoggerProcessEnvironment)process).Logger.Log(EverestLogLevel.Debug,
                            "MaxHelpingHand/CustomizableCrumblePlatform",
                            $"A block was removed: invalidating group for {platforms.Count} crumble blocks!");
                        foreach (CustomizableCrumblePlatform platform in platforms) platform.groupMembers = null;
                    };
                    yield break;
                }
                else
                {
                    // wait for the custom delay instead of 2 seconds.
                    yield return respawnDelay;
                }

                // wait if something is where the platform is supposed to respawn
                while (isGroupCollidingWithSomething() || IsActorOrSolidAtPlatform(false) ||
                       IsActorOrSolidAtPlatform(true))
                {
                    yield return null;
                }

                // make the platform reappear
                CrumbleOutlineFader.Replace(RunCrumbleOutlineFade(0f));
                CrumbleOccluder.Visible = true;
                Collidable = true;
                if (attachStaticMovers)
                {
                    EnableStaticMovers();
                }

                if (flagMode == FlagMode.UntilPlatformRespawn)
                {
                    ((OriginalLevelSceneRoot)Scene).Session.SetFlag(flag, flagInverted);
                }

                for (int m = 0; m < 4; m++)
                {
                    for (int i = 0; i < CrumbleImages.Count; i++)
                    {
                        if (i % 4 - m == 0)
                        {
                            // only emit sound if we should emit sound no matter what, or if the current platform is the one that got triggered.
                            if (!onlyEmitSoundForPlayer || triggeredPlatform == this)
                            {
                                CrumbleFalls[i]
                                    .Replace(RunCrumbleTileIn(i, CrumbleImages[CrumbleFallOrder[i]], 0.05f * m));
                            }
                            else
                            {
                                CrumbleFalls[i].Replace(RunCrumbleTileInWithoutSound(i,
                                    CrumbleImages[CrumbleFallOrder[i]], 0.05f * m));
                            }
                        }
                    }
                }
            }
        }
    }
}
