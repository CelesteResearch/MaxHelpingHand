using System.Numerics;
using Celeste.Core.Audio;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Tracking.Attributes;
using Celeste.Game.Audio.Surfaces;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Walls.Concealment;

namespace Celeste.Mods.MaxHelpingHand.Entities
{
    [CustomEntity("MaxHelpingHand/FlagExitBlock")]
    [EverestTrackedAs(typeof(ExitBlockState))]
    [global::Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.FlagExitBlock")]
    public sealed class FlagExitBlock : ExitBlockState
    {
        private readonly string flag;
        private readonly bool inverted;
        private readonly bool playSound;
        private readonly bool instant;

        public static FlagExitBlock Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new FlagExitBlock(level, data, offset);

        private FlagExitBlock(OriginalLevelSceneRoot level, OriginalLevelEntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, data.Char("tiletype", '3'),
                level.SolidWorldFactory.Create(data.Position + offset, true), new SecretWallContext
                {
                    PlaySound = (name, position) =>
                        ((ISourceSpatialAudioEnvironment)level.EngineContext).SpatialAudio.Play(name, position),
                    TileToSurfaceSoundIndex = tile =>
                        ((IOriginalTileSurfaceEnvironment)level.EngineContext).SurfaceIndices.TileToIndex[tile]
                })
        {
            flag = data.Attr("flag");
            inverted = data.Bool("inverted");
            playSound = data.Bool("playSound");
            instant = data.Bool("instant");
            Remove(TransitionListener);
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            if (((OriginalLevelSceneRoot)Scene).Session.GetFlag(flag) == inverted)
            {
                Cutout.Alpha = Tiles.Alpha = 0f;
                Collidable = false;
            }
        }

        public override void Update()
        {
            UpdateSolid();
            bool wasCollidable = Collidable;
            Collidable = ((OriginalLevelSceneRoot)Scene).Session.GetFlag(flag) != inverted &&
                         !SourceTrackedEntityCollision.Check(((ISourceScene)Scene).SourceScene.Entities,
                             typeof(global::Celeste.Simulation.RoomPlayerSession).FullName, this);
            if (playSound && !wasCollidable && Collidable)
                ((ISourceSpatialAudioEnvironment)SourceWorld.EngineContext).SpatialAudio.Play(
                    "event:/game/general/passage_closed_behind", Center);
            Cutout.Alpha = Tiles.Alpha = ScalarMath.Approach(Tiles.Alpha, Collidable ? 1f : 0f,
                instant ? 1f : SourceWorld.EngineContext.DeltaTime,
                ((ISourceExtremaEnvironment)SourceWorld.EngineContext).ExtremaArithmetic);
        }
    }
}
