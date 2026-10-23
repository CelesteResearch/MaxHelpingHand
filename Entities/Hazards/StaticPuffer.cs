// Copyright (c) 2019 maddie480. Licensed under the MIT License.

using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Hazards.Puffer;
using Celeste.Physics.Collision;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.MaxHelpingHand.Entities.Hazards
{
    [CustomEntity("MaxHelpingHand/StaticPuffer")]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.StaticPuffer")]
    public class StaticPuffer : OriginalPufferState
    {
        private int downboostTolerance;
        internal int DownboostTolerance => downboostTolerance;
        internal bool FacesRight => PufferFacing.X > 0;

        public StaticPuffer(OriginalLevelSceneRoot level, EntityData data, Vector2 offset)
            : base(new PixelActor(((ISourcePlatformCollisionScene)level).PlatformWorld, data.Position + offset),
                data.Position + offset, data.Bool("right"), level.EngineContext,
                ((ISourcePlatformTargetMotionEnvironment)level.EngineContext).TargetMotionPolicy)
        {
            PufferIdleSine.RemoveSelf();
            Depth = -1;
            downboostTolerance = data.Int("downboostTolerance", -1);
        }

        public static StaticPuffer Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new StaticPuffer(level, data, offset);

        internal void ResetIdleSine() => PufferIdleSine.Reset();
    }
}
