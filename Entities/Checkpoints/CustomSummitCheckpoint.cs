using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Gameplay.Checkpoints.Summit;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Display.Colors;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;

namespace Celeste.Mods.MaxHelpingHand.Entities.Checkpoints
{
    [CustomEntity("MaxHelpingHand/CustomSummitCheckpoint = GenerateCustomSummitCheckpoint")]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.CustomSummitCheckpoint")]
    [SceneTracked]
    public partial class CustomSummitCheckpoint : OriginalSummitCheckpointState
    {
        private readonly ISourceEngineContext engine;
        private readonly PackedColor[] confettiColors;
        private readonly string groupFlag;

        public static CustomSummitCheckpoint GenerateCustomSummitCheckpoint(OriginalLevelSceneRoot level,
            OriginalLevelRoomData room, Vector2 offset, OriginalLevelEntityData data)
        {
            data.Values["number"] = data.ID + 100;
            return new CustomSummitCheckpoint(level.EngineContext, data, offset);
        }

        public CustomSummitCheckpoint(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(engine, data,
            offset)
        {
            this.engine = engine;
            confettiColors = ParseColors(data.Attr("confettiColors", "fe2074,205efe,cefe20"));
            groupFlag = data.Attr("groupFlag");
            string first = data.Attr("firstDigit"), second = data.Attr("secondDigit");
            string directory = data.Attr("spriteDirectory", "MaxHelpingHand/summitcheckpoints");
            SetNumberString(this, "01");
            var atlas = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas;
            SetNumbersEmpty(this, new List<AtlasEntry>
                { atlas[$"{directory}/{first}/numberbg"], atlas[$"{directory}/{second}/numberbg"] });
            SetNumbersActive(this, new List<AtlasEntry>
                { atlas[$"{directory}/{first}/number"], atlas[$"{directory}/{second}/number"] });
            SetBaseEmpty(this, atlas[$"{directory}/base00"]);
            SetBaseToggle(this, atlas[$"{directory}/base01"]);
            SetBaseActive(this, atlas[$"{directory}/base02"]);
        }

        private static PackedColor[] ParseColors(string input)
        {
            string[] source = input.Split(',');
            var result = new PackedColor[source.Length];
            for (int i = 0; i < result.Length; i++) result[i] = SourceHexColor.HexToColor(source[i]);
            return result;
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            if (!string.IsNullOrEmpty(groupFlag) && ((OriginalLevelSceneRoot)scene).Session.GetFlag(groupFlag))
                Activated = true;
        }

        public override void Awake(object scene)
        {
            bool wasActivated = Activated;
            base.Awake(scene);
            if (!wasActivated && Activated && !string.IsNullOrEmpty(groupFlag)) TriggerGroup(false);
        }

        public override void Update()
        {
            bool wasActivated = Activated;
            if (!Activated && CollideCheck(typeof(Celeste.Simulation.RoomPlayerSession).FullName))
            {
                var previous = GetConfettiColors();
                SetConfettiColors(confettiColors);
                base.Update();
                SetConfettiColors(previous);
            }
            else base.Update();

            if (!wasActivated && Activated && !string.IsNullOrEmpty(groupFlag)) TriggerGroup(true);
        }

        private void TriggerGroup(bool effects)
        {
            var level = (OriginalLevelSceneRoot)Scene;
            level.Session.SetFlag(groupFlag);
            foreach (CustomSummitCheckpoint other in ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(
                         typeof(CustomSummitCheckpoint)))
            {
                if (other.Activated || other.groupFlag != groupFlag) continue;
                other.Activated = true;
                if (!effects) continue;
                var otherLevel = (OriginalLevelSceneRoot)other.Scene;
                otherLevel.Displacement.AddBurst(other.Position, .5f, 4f, 24f, .5f);
                var previous = GetConfettiColors();
                SetConfettiColors(other.confettiColors);
                otherLevel.SourceScene.QueueSourceEntity(new ConfettiRenderer(other.engine, other.Position));
                SetConfettiColors(previous);
                ((IOriginalGameplayProcess)engine).AudioSession.Play("event:/game/07_summit/checkpoint_confetti",
                    other.Position, ((IOriginalGameplayProcess)engine).SaveContext);
            }
        }
    }
}
