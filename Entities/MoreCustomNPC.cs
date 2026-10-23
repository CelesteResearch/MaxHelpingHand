using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Core.Geometry;
using Celeste.Core.Maps;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Core.Text.Sequences;
using Celeste.Everest.Diagnostics.Logging;
using Celeste.Everest.Entities.Npcs;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Game.Content.Dialogs;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Graphics;
using Celeste.Game.Execution.Levels;
using Celeste.Game.UI.Dialogue.Textboxes;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Mods.MaxHelpingHand.Modules;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Runtime.Session;

namespace Celeste.Mods.MaxHelpingHand.Entities
{
    [CustomEntity("MaxHelpingHand/MoreCustomNPC", "MaxHelpingHand/CustomNPCSprite")]
    [SceneTracked]
    [global::Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.MoreCustomNPC")]
    public class MoreCustomNPC : EverestCustomNpcState
    {
        private readonly IntRect? talkerZone;
        private readonly bool hasDialogue;
        private readonly string onlyIfFlag;
        private readonly bool onlyIfFlagInverted;
        private readonly bool autoSkipEnabled;
        private readonly string customFont;
        private readonly Sprite customSprite;
        private readonly string spriteName;

        public MoreCustomNPC(OriginalLevelEntityData data, Vector2 offset, MapEntityId id, ISourceEngineContext engine)
            : base(data, offset, id, engine)
        {
            hasDialogue = !string.IsNullOrEmpty(data.Attr("dialogId"));
            onlyIfFlag = data.Attr("onlyIfFlag");
            onlyIfFlagInverted = data.Bool("onlyIfFlagInverted");
            autoSkipEnabled = data.Bool("autoSkipEnabled");
            customFont = data.Attr("customFont");
            spriteName = data.Attr("spriteName", "");
            if (!string.IsNullOrEmpty(spriteName))
            {
                TextureFrames = null;
                customSprite = ((OriginalGameGraphicsResources)((ISourceGameGraphicsEnvironment)engine).GameGraphics)
                    .SpriteBank.Create(spriteName);
                customSprite.Scale = NpcScale;
                OwnedComponents.Add(customSprite);
            }

            string frames = data.Attr("frames", "");
            if (!string.IsNullOrEmpty(frames))
            {
                var all = new List<Celeste.Graphics.Content.Atlas.AtlasEntry>(TextureFrames);
                TextureFrames = new List<Celeste.Graphics.Content.Atlas.AtlasEntry>();
                foreach (int frame in SourceIntegerSequenceParsing.ReadCsvWithRangesAndRepetition(frames))
                    TextureFrames.Add(all[frame]);
            }

            if (data.Nodes.Length >= 2)
            {
                var nodes = data.NodesOffset(offset);
                float top = Math.Min(nodes[0].Y, nodes[1].Y);
                float bottom = Math.Max(nodes[0].Y, nodes[1].Y) + 8;
                float left = Math.Min(nodes[0].X, nodes[1].X);
                float right = Math.Max(nodes[0].X, nodes[1].X) + 8;
                talkerZone = new IntRect((int)(left - Position.X), (int)(top - Position.Y),
                    (int)(right - left), (int)(bottom - top));
            }

            string setFlag = data.Attr("setFlag");
            bool setFlagInverted = data.Bool("setFlagInverted");
            if (!string.IsNullOrEmpty(setFlag))
                OnEnd += () =>
                {
                    if (OwnedComponents.IsPendingRemoval(Talker) || Talker.Entity == null
                                                                 || Session.GetCounter(id + "DialogCounter") == 0)
                        Session.SetFlag(setFlag, !setFlagInverted);
                };
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            if (talkerZone.HasValue && Talker != null) Talker.Bounds = talkerZone.Value;
            if (!hasDialogue && Talker != null) OwnedComponents.Remove(Talker);
        }

        public override void Update()
        {
            base.Update();
            if (!string.IsNullOrEmpty(onlyIfFlag) && Talker?.Entity != null)
                Talker.Enabled = Session.GetFlag(onlyIfFlag) != onlyIfFlagInverted;
        }

        public override void Render(ISourceRenderContext context)
        {
            base.Render(context);
            customSprite?.Render(context);
        }

        protected override IEnumerator CreateDialogueRoutine(string dialog)
        {
            Func<IEnumerator>[] events =
                autoSkipEnabled ? new Func<IEnumerator>[] { StartSkipping, StopSkipping } : null;
            if (string.IsNullOrEmpty(customFont)) return TextboxState.Say(dialog, Engine, events);
            return DialogueWithFont(dialog, events);
        }

        private IEnumerator DialogueWithFont(string dialog, Func<IEnumerator>[] events)
        {
            var fonts = ((IOriginalGameTextEnvironment)Engine).Text.Fonts;
            if (fonts.Get(customFont) == null)
            {
                if (!fonts.ContainsFace(customFont))
                {
                    Warn($"We need to list fonts again, {customFont} does not exist!");
                    fonts.Prepare();
                }

                fonts.Load(customFont);
                ((ISourceScene)Scene).SourceScene.QueueSourceEntity(new NpcFontHolder(Engine, fonts, customFont));
            }

            var routine = TextboxState.SayWithLayoutFont(dialog, Engine, customFont, events);
            routine.MoveNext();
            yield return routine.Current;
            yield return new SourceSwapImmediately(routine);
        }

        private IEnumerator StartSkipping()
        {
            var settings = ((ISourceEverestModulesEnvironment)Engine).Modules.OfType<MaxHelpingHandModule>().Single()
                .Settings;
            GetTextbox().AutoPressContinue = !settings.DisableDialogueAutoSkip;
            yield break;
        }

        private IEnumerator StopSkipping()
        {
            GetTextbox().AutoPressContinue = false;
            yield break;
        }

        private TextboxState GetTextbox() =>
            ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntity<TextboxState>();

        public static MoreCustomNPC GetNPC(ISourceEngineContext engine, string spriteName)
        {
            foreach (MoreCustomNPC npc in ((ISourceScene)engine.Frame.Scenes.CurrentScene).SourceScene.Entities
                     .GetTrackedEntities(typeof(MoreCustomNPC)))
                if (npc.spriteName == spriteName)
                    return npc;
            ((IEverestLoggerProcessEnvironment)engine).Logger.Warn("MaxHelpingHand/MoreCustomNPC",
                $"Custom NPC with sprite name {spriteName} not found!");
            return null;
        }

        public void PlayAnimation(string name)
        {
            if (customSprite == null) Warn("Tried to play an animation on a non-sprite-based custom NPC!");
            else if (!customSprite.Has(name)) Warn($"Tried to play non-existent animation {name}!");
            else customSprite.Play(name);
        }

        public void SetHorizontalScale(float scale)
        {
            if (customSprite == null) NpcScale = new Vector2(scale, NpcScale.Y);
            else customSprite.Scale.X = scale;
        }

        public Sprite GetSprite()
        {
            if (customSprite == null) Warn("This NPC does not use a sprite!");
            return customSprite;
        }

        private void Warn(string message) =>
            ((IEverestLoggerProcessEnvironment)Engine).Logger.Warn("MaxHelpingHand/MoreCustomNPC", message);

        public new static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
            => new MoreCustomNPC(data, offset, level.CreateEntityId(room, data), level.EngineContext);
    }
}
