// SPDX-License-Identifier: MIT

using System.Numerics;
using Celeste.Core.Mathematics;
using Celeste.Game.Entities.Platforms.Wood;
using Celeste.Game.Execution.Levels;

namespace Celeste.Mods.MaxHelpingHand.Entities.Platforms
{
    public partial class MultiNodeMovingPlatform
    {
        public override void Update()
        {
            base.Update();
            Collidable = Visible;
            if (HasPlayerRider())
            {
                sinkTimer = .2f;
                addY = ScalarMath.Approach(addY, 3f * sinkingDir, 50f * engine.DeltaTime);
            }
            else if (sinkTimer > 0f)
            {
                sinkTimer -= engine.DeltaTime;
                addY = ScalarMath.Approach(addY, 3f * sinkingDir, 50f * engine.DeltaTime);
            }
            else addY = ScalarMath.Approach(addY, 0f, 20f * engine.DeltaTime);

            if (!string.IsNullOrEmpty(flag) && !((OriginalLevelSceneRoot)Scene).Session.GetFlag(flag))
            {
                moveToPosition(previousPosition + new Vector2(0f, addY));
                return;
            }

            if (pauseTimer > 0f)
            {
                pauseTimer -= engine.DeltaTime;
                moveToPosition(nodes[prevNodeIndex] + new Vector2(0f, addY));
                return;
            }

            if (percent == 0f && emitSound)
                sfx.Play(lastSfx = lastSfx == OriginalMovingPlatformState.SfxLeft
                    ? OriginalMovingPlatformState.SfxRight
                    : OriginalMovingPlatformState.SfxLeft);
            if (accurateTiming)
            {
                if (percentRemainder.HasValue)
                {
                    percent += percentRemainder.Value;
                    percentRemainder = null;
                }

                percent += engine.DeltaTime / moveTime;
                if (percent >= 1f)
                {
                    percentRemainder = percent - 1f;
                    percent = 1f;
                }
            }
            else percent = ScalarMath.Approach(percent, 1f, engine.DeltaTime / moveTime);

            updatePosition();
        }

        private void updatePosition()
        {
            if (mode is Mode.BackAndForthNoPause or Mode.LoopNoPause)
            {
                if (percent == 1f)
                {
                    percent = 0f;
                    pauseTimer = pauseTime;
                    if (mode == Mode.BackAndForthNoPause)
                    {
                        prevNodeIndex = direction == 1 ? nodes.Length - 1 : 0;
                        direction = -direction;
                    }

                    moveToPosition(nodes[prevNodeIndex] + new Vector2(0f, addY));
                }
                else
                {
                    float progress = applyEase(direction == 1 ? percent : 1f - percent);
                    int i;
                    for (i = 0; nodePercentages[i] < progress; i++)
                    {
                    }

                    float previous = i == 0 ? 0f : nodePercentages[i - 1];
                    float next = nodePercentages[i];
                    moveToPosition(Vector2Interpolation.Lerp(nodes[i], nodes[(i + 1) % nodes.Length],
                        ScalarMath.ClampedMap(progress, previous, next), scalar) + new Vector2(0f, addY));
                }

                return;
            }

            moveToPosition(
                Vector2Interpolation.Lerp(nodes[prevNodeIndex], nodes[nextNodeIndex], applyEase(percent), scalar)
                + new Vector2(0f, addY));
            if (percent != 1f) return;
            prevNodeIndex = nextNodeIndex;
            nextNodeIndex = prevNodeIndex + direction;
            if (nextNodeIndex < 0)
            {
                nextNodeIndex = 1;
                direction = 1;
            }
            else if (nextNodeIndex >= nodes.Length)
            {
                if (mode == Mode.Loop) nextNodeIndex = 0;
                else if (mode == Mode.TeleportBack)
                {
                    prevNodeIndex = 0;
                    nextNodeIndex = 1;
                    teleporting = true;
                }
                else if (mode == Mode.BackAndForth)
                {
                    nextNodeIndex -= 2;
                    direction = -1;
                }
            }

            percent = 0f;
            pauseTimer = pauseTime;
        }

        private void moveToPosition(Vector2 position)
        {
            movingHorizontally = position.Y == ExactPosition.Y;
            if (teleporting) MoveToNaive(position);
            else MoveTo(position);
            previousPosition = position - new Vector2(0f, addY);
            teleporting = false;
        }

        private float applyEase(float value) => easing ? sineInOut(value) : value;
    }
}
