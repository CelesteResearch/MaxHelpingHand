using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.MaxHelpingHand.Modules;
using Celeste.Mods.MaxHelpingHand.Presentation.Backdrops;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking.Attributes;

namespace Celeste.Mods.MaxHelpingHand.Entities.Backdrops
{
    [CustomEntity("MaxHelpingHand/StylegroundFadeController")]
    [SceneTracked]
    public class StylegroundFadeController : SceneEntityState
    {
        internal readonly string[] Flags;
        internal readonly bool NotFlag;
        internal readonly float FadeInTime;
        internal readonly float FadeOutTime;
        private readonly MaxHelpingHandModule module;
        private StylegroundFadeState fades;

        public StylegroundFadeController(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            Flags = data.Attr("flag").Split(',');
            NotFlag = data.Bool("notFlag");
            FadeInTime = data.Float("fadeInTime");
            FadeOutTime = data.Float("fadeOutTime");
            module = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<MaxHelpingHandModule>().Single();
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            Register();
        }

        private void Register()
        {
            fades = module.GetStylegroundFades((OriginalLevelSceneRoot)Scene);
            fades.Register(this);
        }

        public override void Update()
        {
            base.Update();
            if (fades == null || !fades.Contains(this)) Register();
        }

        public override void Removed(object scene)
        {
            base.Removed(scene);
            Deregister();
        }

        public override void SceneEnd(object scene)
        {
            base.SceneEnd(scene);
            Deregister();
        }

        private void Deregister()
        {
            if (fades == null) return;
            fades.Deregister(this);
            module.ReleaseStylegroundFades(fades);
            fades = null;
        }
    }
}
