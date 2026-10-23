using System;
using Celeste.Core.Display;
using System.Numerics;
using Celeste.Core.Mathematics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Content.Maps.Processing;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Tracking.Attributes;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Content.Maps;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Entities.Collectibles.Hearts;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Display.Colors;
using Celeste.Mods.MaxHelpingHand.Runtime.Collectibles.Hearts;
using Celeste.Simulation.Presentation.Particles.Definitions;
using Celeste.Game.Execution.Processes;

namespace Celeste.Mods.MaxHelpingHand.Entities.Collectibles.Hearts
{
    [CustomEntity("MaxHelpingHand/ReskinnableCrystalHeart")]
    [EverestTrackedAs(typeof(OriginalHeartGemState))]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.ReskinnableCrystalHeart")]
    public class ReskinnableCrystalHeart : OriginalHeartGemState
    {
        private readonly ISourceEngineContext engine;
        private readonly string overrideSprite;
        private readonly string ghostSprite;
        private readonly string particleColor;
        private readonly string flagOnCollect;
        private readonly bool flagInverted;
        private readonly bool disableGhostSprite;
        private float _rotationDegrees;

        public float rotationDegrees
        {
            get => _rotationDegrees;
            set
            {
                _rotationDegrees = value;
                var sprite = HeartGemFields.GetSprite(this);
                if (sprite != null) sprite.Rotation = _rotationDegrees * 0.017453292f;
            }
        }

        public ReskinnableCrystalHeart(ISourceEngineContext engine, OriginalLevelEntityData data, Vector2 offset)
            : base(engine, data, offset)
        {
            this.engine = engine;
            overrideSprite = EntityAttributeReader.Attr(data.Values, "sprite");
            ghostSprite = EntityAttributeReader.Attr(data.Values, "ghostSprite");
            particleColor = EntityAttributeReader.Attr(data.Values, "particleColor");
            flagOnCollect = EntityAttributeReader.Attr(data.Values, "flagOnCollect");
            flagInverted = EntityAttributeReader.Bool(data.Values, "flagInverted");
            disableGhostSprite = EntityAttributeReader.Bool(data.Values, "disableGhostSprite");
            _rotationDegrees = EntityAttributeReader.Float(data.Values, "rotation");
        }

        public static ReskinnableCrystalHeart Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) =>
            new ReskinnableCrystalHeart(level.EngineContext, data, offset);

        public override void Added(object scene)
        {
            base.Added(scene);
            var level = (OriginalLevelSceneRoot)Scene;
            var meta = ((IEverestChapterMapProcessingEnvironment)engine).MapProcessing.GetModeMeta(
                (OriginalChapterMapData)level.Session.MapData);
            bool heartIsEnd = meta != null && meta.HeartIsEnd.GetValueOrDefault();
            if (level.Session.HeartGem && !heartIsEnd) RemoveSelf();
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            if (!string.IsNullOrEmpty(particleColor))
                HeartGemFields.SetShine(this,
                    new ParticleTypePresentation(((IOriginalGameplayProcess)engine).ParticleTypes.HeartGem.P_BlueShine)
                        { Color = PackedColor.FromHex(particleColor) });
            HeartGemFields.GetSprite(this).Rotation = _rotationDegrees * 0.017453292f;
        }

        public Vector2 GetRotationVector(float length) =>
            Vector2Geometry.AngleToVector(-1.5707964f + rotationDegrees * 0.017453292f, length);

        internal bool MatchesEngine(ISourceEngineContext value) => ReferenceEquals(engine, value);
        internal bool SelectGhost(bool original) => disableGhostSprite ? false : original;

        internal string SelectSprite(string original) => !IsGhost && !string.IsNullOrEmpty(overrideSprite)
            ? overrideSprite
            : IsGhost && !string.IsNullOrEmpty(ghostSprite)
                ? ghostSprite
                : original;

        internal void BeforeCollect()
        {
            if (!string.IsNullOrEmpty(flagOnCollect))
                ((OriginalLevelSceneRoot)Scene).Session.SetFlag(flagOnCollect, !flagInverted);
        }

        internal void RotatedWiggle()
        {
            HeartGemFields.GetSprite(this).Position =
                GetRotationVector(MathF.Sin(HeartGemFields.GetTimer(this) * 2f) * 2f) +
                HeartGemFields.GetMoveDirection(this) * (HeartGemFields.GetMoveWiggler(this).Value * -8f);
        }
    }
}
