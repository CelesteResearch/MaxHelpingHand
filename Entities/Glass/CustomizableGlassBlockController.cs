// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.LevelLoader;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.MaxHelpingHand.Modules;
using Celeste.Mods.MaxHelpingHand.Sessions;
using Celeste.Physics.Collision;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;

namespace Celeste.Mods.MaxHelpingHand.Entities.Glass
{
    [CustomEntity("MaxHelpingHand/CustomizableGlassBlockController")]
    [SceneTracked(inherited: true)]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.CustomizableGlassBlockController")]
    public class CustomizableGlassBlockController : ChapterGlassBlockBackgroundSceneState
    {
        public CustomizableGlassBlockController(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(engine, false, false, data.Bool("wavy", false))
        {
            Position = data.Position + offset;
            starColors = data.Attr("starColors").Split(',').Select(PackedColor.FromHex).ToArray();
            bgColor = PackedColor.FromHex(data.Attr("bgColor"));
            FadeInRate = 4f;
            var module = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<MaxHelpingHandModule>().Single();
            module.Session.GlassBlockCurrentSettings = data.Bool("persistent")
                ? new GlassBlockSettings
                {
                    StarColors = data.Attr("starColors"), BgColor = data.Attr("bgColor"),
                    Wavy = data.Bool("wavy", false)
                }
                : null;
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            var previous = ((ISourceScene)scene).SourceScene.Entities
                .GetTrackedEntities(typeof(CustomizableGlassBlockController))
                .OfType<CustomizableGlassBlockController>().FirstOrDefault(controller =>
                    controller != this && controller is not CustomizableGlassBlockAreaController);
            if (previous != null) CopyVisualStateFrom(previous);
            else InitializeVisualState();
        }

        protected override IReadOnlyList<object> GetGlassBlocks() =>
            ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(typeof(CustomizableGlassBlock));

        protected override Vector2 BlockShake(ISourceEntityCollider block) =>
            ((CustomizableGlassBlock)block).ShakeVector;

        protected override float BlockAlpha(ISourceEntityCollider block) => ((CustomizableGlassBlock)block).Alpha;
        protected override Vector2 DisplacementInset => Vector2.One;
        protected override string SurfaceTargetName => "customizable-glass-block-surfaces";
        protected override string BeamTargetName => "customizable-glass-block-beams";
    }
}
