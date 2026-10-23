using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Physics.Collision;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.MaxHelpingHand.Triggers
{
    [CustomEntity("MaxHelpingHand/ColorGradeFadeTrigger")]
    [SceneTracked]
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Triggers.ColorGradeFadeTrigger")]
    public sealed class ColorGradeFadeTrigger : SourceTriggerEntityState
    {
        private readonly string colorGradeA;
        private readonly string colorGradeB;
        private readonly PositionModes direction;
        private readonly bool evenDuringReflectionFall;

        public ColorGradeFadeTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
                data.Position + offset, data.Width, data.Height,
                ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            colorGradeA = data.Attr("colorGradeA");
            colorGradeB = data.Attr("colorGradeB");
            direction = data.Enum<PositionModes>("direction");
            evenDuringReflectionFall = data.Bool("evenDuringReflectionFall", true);
        }

        public override void Update()
        {
            base.Update();
            if (!evenDuringReflectionFall) return;
            RoomPlayerSession player = null;
            foreach (RoomPlayerSession entry in ((ISourceScene)Scene).SourceScene.Entities
                     .GetTrackedEntities(typeof(RoomPlayerSession)))
            {
                player = entry;
                break;
            }

            if (player == null || player.Machine.State != 18) return;
            player.PlayerTriggers.UpdateContact(player, this, SourceEntityCollision.Check(this, player));
        }

        public override void OnStay(RoomPlayerSession player)
        {
            var level = (OriginalLevelSceneRoot)Scene;
            float positionLerp = GetPositionLerp(player, direction);
            if (positionLerp > 0.5f)
            {
                level.ColorGrade.LastGrade = colorGradeA;
                level.Session.ColorGrade = colorGradeB;
                level.ColorGrade.Ease = positionLerp;
            }
            else
            {
                level.ColorGrade.LastGrade = colorGradeB;
                level.Session.ColorGrade = colorGradeA;
                level.ColorGrade.Ease = 1 - positionLerp;
            }

            level.ColorGrade.EaseSpeed = 1f;
        }
    }
}
