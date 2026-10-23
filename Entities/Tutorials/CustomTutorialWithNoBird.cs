using System.Collections;
using System.Numerics;
using Celeste.Everest.Entities.Birds.Tutorials;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Tracking.Attributes;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Entities;
using Celeste.Simulation.Presentation.Hud.Tutorials.Birds;

namespace Celeste.Mods.MaxHelpingHand.Entities.Tutorials
{
    [CustomEntity("MaxHelpingHand/CustomTutorialWithNoBird")]
    [EverestTrackedAs(typeof(CustomBirdTutorial))]
    public class CustomTutorialWithNoBird : CustomBirdTutorial
    {
        public CustomTutorialWithNoBird(OriginalLevelSceneRoot level, OriginalLevelEntityData data, Vector2 offset)
            : base(level, data, offset)
        {
            var direction = data.Bool("hasPointer", true)
                ? data.Enum("direction", BirdTutorialPointerDirection.Down)
                : BirdTutorialPointerDirection.None;
            CustomTutorialGui.PointerDirection = direction;
            CustomTutorialGui.MirrorPointer = direction == BirdTutorialPointerDirection.Down;
            OwnedComponents.Remove(Sprite);
        }

        // The upstream hook deliberately suppresses all startle/flight effects for this entity.
        public override IEnumerator StartleAndFlyAway()
        {
            yield break;
        }

        public new static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
            => new CustomTutorialWithNoBird(level, data, offset);
    }
}
