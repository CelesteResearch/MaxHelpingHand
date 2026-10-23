using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Mods.MaxHelpingHand.Runtime.Backdrops;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;

namespace Celeste.Mods.MaxHelpingHand.Entities.Backdrops
{
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias(
        "Celeste.Mod.MaxHelpingHand.Entities.ParallaxFadeSpeedController")]
    [CustomEntity("MaxHelpingHand/ParallaxFadeSpeedController")]
    [SceneTracked(false)]
    public sealed class ParallaxFadeSpeedController : SceneEntityState
    {
        internal readonly float FadeTime;
        private readonly ParallaxFadeSpeedHooks owner;

        public ParallaxFadeSpeedController(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
            Position = data.Position + offset;
            FadeTime = data.Float("fadeTime", 1f);
            owner = ParallaxFadeSpeedHooks.For(engine);
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            owner.Enabled = true;
        }

        public override void Removed(object scene)
        {
            base.Removed(scene);
            if (((ISourceScene)scene).SourceScene.Entities.GetTrackedEntities(owner.Key).Count <= 1)
                owner.Enabled = false;
        }

        public override void SceneEnd(object scene)
        {
            base.SceneEnd(scene);
            owner.Enabled = false;
        }
    }
}
