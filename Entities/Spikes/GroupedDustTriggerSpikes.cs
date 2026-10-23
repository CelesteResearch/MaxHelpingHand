// SPDX-License-Identifier: MIT

using System.Numerics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Hazards.TriggerSpikes;

namespace Celeste.Mods.MaxHelpingHand.Entities.Spikes
{
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.GroupedDustTriggerSpikes")]
    public class GroupedDustTriggerSpikes : TriggerSpikesState, ISourceEngineContextEntity
    {
        private readonly bool triggerIfSameDirection;
        private readonly bool killIfSameDirection;
        private bool triggered;
        public ISourceEngineContext EngineContext { get; }

        public GroupedDustTriggerSpikes(EntityData data, Vector2 offset, TriggerSpikesDirection direction,
            ISourceEngineContext engine)
            : base(data.Position + offset, direction <= TriggerSpikesDirection.Down ? data.Width : data.Height,
                direction, engine)
        {
            EngineContext = engine;
            triggerIfSameDirection = EntityAttributeReader.Bool(data.Values, "triggerIfSameDirection", false);
            killIfSameDirection =
                EntityAttributeReader.Bool(data.Values, "killIfSameDirection", triggerIfSameDirection);
        }

        protected override void GetPlayerCollideIndex(RoomPlayerSession player, out int minIndex, out int maxIndex)
        {
            Vector2 initialPlayerSpeed = player.Data.Motion.Normal.Speed;
            bool sameDirection = triggered ? killIfSameDirection : triggerIfSameDirection;
            if (sameDirection) player.Data.Motion.Normal.Speed = Vector2.Zero;
            base.GetPlayerCollideIndex(player, out minIndex, out maxIndex);
            if (sameDirection) player.Data.Motion.Normal.Speed = initialPlayerSpeed;
            int spikeCount = SourceSize / 4;
            if (maxIndex >= 0 && minIndex < spikeCount)
            {
                minIndex = 0;
                maxIndex = spikeCount - 1;
                triggered = true;
            }
        }
    }
}
