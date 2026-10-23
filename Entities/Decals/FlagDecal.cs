using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Scene.Entities;

namespace Celeste.Mods.MaxHelpingHand.Entities.Decals
{
    [CustomEntity("MaxHelpingHand/FlagDecal = Load", "MaxHelpingHand/FlagDecalXML = Load")]
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.FlagDecal")]
    public sealed class FlagDecal : SceneEntityState
    {
        private readonly Sprite sprite;
        private readonly string flag;
        private readonly bool inverted;
        private bool activated;

        public static FlagDecal Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(level.EngineContext, data, offset);

        public FlagDecal(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            flag = data.Attr("flag");
            inverted = data.Bool("inverted");
            Depth = data.Int("depth");
            var graphics = ((ISourceGameGraphicsEnvironment)engine).GameGraphics;
            if (data.Has("sprite"))
            {
                sprite = graphics.SpriteFactory.Create(data.Attr("sprite"));
            }
            else
            {
                float animationDelay = 1f / data.Float("fps");
                sprite = new Sprite(engine, graphics.GameAtlas, "decals/");
                sprite.AddLoop("enabled", data.Attr("decalPath"), animationDelay);
                if (!string.IsNullOrEmpty(data.Attr("appearAnimationPath")))
                    sprite.Add("appear", data.Attr("appearAnimationPath"), animationDelay, "enabled");
                if (!string.IsNullOrEmpty(data.Attr("disappearAnimationPath")))
                    sprite.Add("disappear", data.Attr("disappearAnimationPath"), animationDelay);
                sprite.CenterOrigin();
            }

            sprite.OnFinish += animation =>
            {
                if (animation == "disappear") Hide();
            };
            sprite.Scale = new Vector2(data.Float("scaleX", 1f), data.Float("scaleY", 1f));
            sprite.Rotation = (float)(data.Float("rotation", 0f) * Math.PI / 180f);
            Add(sprite);
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            activated = ((OriginalLevelSceneRoot)Scene).Session.GetFlag(flag) != inverted;
            if (activated) sprite.Play("enabled");
            else Hide();
        }

        public override void Update()
        {
            base.Update();
            bool currentlyActive = ((OriginalLevelSceneRoot)Scene).Session.GetFlag(flag) != inverted;
            if (currentlyActive == activated) return;
            if (currentlyActive)
            {
                Visible = true;
                sprite.Play(sprite.Has("appear") ? "appear" : "enabled");
            }
            else if (sprite.Has("disappear")) sprite.Play("disappear");
            else Hide();

            activated = currentlyActive;
        }

        private void Hide()
        {
            if (sprite.Has("disabled")) sprite.Play("disabled");
            else Visible = false;
        }
    }
}
