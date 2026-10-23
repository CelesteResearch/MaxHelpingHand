// SPDX-License-Identifier: MIT

using System.Numerics;

namespace Celeste.Mods.MaxHelpingHand.Entities.Platforms
{
    public partial class MultiNodeMovingPlatform
    {
        private void InitializePathPercentages()
        {
            if (mode is not (Mode.BackAndForthNoPause or Mode.LoopNoPause)) return;
            nodePercentages = new float[mode == Mode.LoopNoPause ? nodes.Length : nodes.Length - 1];
            float length = 0f;
            for (int i = 0; i < nodes.Length - 1; i++)
            {
                float distance = vector.Distance(nodes[i], nodes[i + 1]);
                nodePercentages[i] = length + distance;
                length += distance;
            }

            if (mode == Mode.LoopNoPause)
            {
                float distance = vector.Distance(nodes[nodes.Length - 1], nodes[0]);
                nodePercentages[nodes.Length - 1] = length + distance;
                length += distance;
            }

            for (int i = 0; i < nodePercentages.Length; i++) nodePercentages[i] /= length;
        }

        private void InitializeOffset(float offset)
        {
            startingOffset = offset;
            if (offset == 0f) return;
            int segments = mode is Mode.Loop or Mode.LoopNoPause ? nodes.Length : nodes.Length - 1;
            switch (mode)
            {
                case Mode.BackAndForth:
                    if (offset == .5f) offset = .50001f;
                    if (offset > .5f)
                    {
                        offset = (1f - offset) * 2f;
                        int node = (int)(offset * segments);
                        prevNodeIndex = node + 1;
                        nextNodeIndex = node;
                        offset = (offset - (float)node / segments) * segments;
                        percent = 1f - offset;
                        direction = -1;
                    }
                    else
                    {
                        offset *= 2f;
                        int node = prevNodeIndex = (int)(offset * segments);
                        nextNodeIndex = node + 1;
                        offset = (offset - (float)node / segments) * segments;
                        percent = offset;
                    }

                    break;
                case Mode.BackAndForthNoPause:
                    if (offset == .5f) offset = .50001f;
                    if (offset > .5f)
                    {
                        offset = (1f - offset) * 2f;
                        prevNodeIndex = 1;
                        nextNodeIndex = 0;
                        percent = 1f - offset;
                        direction = -1;
                    }
                    else
                    {
                        offset *= 2f;
                        percent = offset;
                    }

                    break;
                case Mode.Loop:
                case Mode.TeleportBack:
                    int index = prevNodeIndex = (int)(offset * segments);
                    nextNodeIndex = (index + 1) % nodes.Length;
                    offset = (offset - (float)index / segments) * segments;
                    percent = offset;
                    break;
                case Mode.LoopNoPause: percent = offset; break;
            }

            startingOffsetIsNotZero = true;
        }
    }
}
