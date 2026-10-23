using Celeste.Core.Components;
using System;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Maps;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Rendering;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Entities.Birds.Tutorials;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Game.Content.Dialogs;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Startup;
using Celeste.Graphics.Display.Colors;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Text.RichText.Drawing;
using Celeste.Graphics.Text.RichText.Layout;
using Celeste.Graphics.Text.RichText.Nodes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Mods.MaxHelpingHand.Runtime.Tutorials;

namespace Celeste.Mods.MaxHelpingHand.Entities.Tutorials
{
    [CustomEntity("MaxHelpingHand/FancyTextTutorial=Load")]
    [SceneTracked]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.FancyTextTutorial")]
    public class FancyTextTutorial : SceneEntityState
    {
        private enum Direction
        {
            Up,
            Down,
            Left,
            Right,
            None
        }

        private static readonly PackedColor bgColor = SourceHexColor.HexToColor("061526");
        private static readonly PackedColor lineColor = new PackedColor(1f, 1f, 1f);
        private float scale;
        private bool triggered;
        private bool open;
        private readonly RichTextLayout fancyText;
        private readonly float fancyTextScale;
        private readonly Vector2 fancyTextDimensions;
        private readonly MapEntityId entityID;
        private readonly Direction direction;
        private readonly string birdId;
        private readonly bool onlyOnce;
        private readonly ISourceEngineContext engine;
        private readonly IOriginalFancyTextProcess process;
        private readonly FancyTextTutorialHooks hooks;

        public static FancyTextTutorial Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data)
            => new(level.EngineContext, data, offset, new MapEntityId(data.Level.Name, data.ID));

        public FancyTextTutorial(ISourceEngineContext engine, OriginalLevelEntityData data, Vector2 offset,
            MapEntityId entityID)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
            this.engine = engine;
            process = (IOriginalFancyTextProcess)engine;
            hooks = FancyTextTutorialHooks.For(engine);
            Position = data.Position + offset;
            fancyText = OriginalFancyTextParser.Parse(process.Text.Dialog.Get(data.Attr("dialogId", "")), int.MaxValue,
                int.MaxValue, 1f, null, OriginalFancyTextContext.Create(process));
            fancyTextScale = data.Float("textScale", 0f);
            direction = data.Enum("direction", Direction.Down);
            birdId = data.Attr("birdId", "");
            onlyOnce = data.Bool("onlyOnce", false);
            this.entityID = entityID;
            int lines = 1;
            foreach (var node in fancyText.Nodes)
                if (node is RichTextLineBreak)
                    lines++;
            fancyTextDimensions =
                new Vector2(fancyText.WidestLine(), lines * fancyText.Font.Get(fancyText.BaseSize).LineHeight) *
                fancyTextScale;
            Tags |= ((IOriginalGameTagsEnvironment)engine).Tags.HUD.Value;
        }

        private int CameraWidth(OriginalLevelSceneRoot level) =>
            hooks.ZoomOutEnabled ? level.Camera.Viewport.Width : 320;

        public override void Update()
        {
            scale = ScalarMath.Approach(scale, open ? 1f : 0f, engine.RawDeltaTime * 8f,
                ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
            base.Update();
        }

        public override void Render(ISourceRenderContext context)
        {
            var scene = Scene;
            var val = scene as OriginalLevelSceneRoot;
            if (val.FrozenOrPaused || val.RetryPlayerCorpse != null || scale <= 0f)
            {
                return;
            }

            var camera = val.Camera;
            Vector2 val2 = Position - Vector2Geometry.Floor(camera.Position);
            if (process.SaveContext.CurrentSave != null && process.SaveContext.CurrentSave.Assists.MirrorMode)
            {
                val2.X = (float)CameraWidth(val) - val2.X;
            }

            val2 *= 1920f / (float)CameraWidth(val);
            float num = (fancyTextDimensions.X + 32f) * scale;
            float num2 = fancyTextDimensions.Y + 32f;
            float num3 = val2.X - num / 2f;
            float num4 = val2.Y - num2 - 32f;
            SourcePrimitiveDrawing.Rect(context, this, num3 - 6f, num4 - 6f, num + 12f, num2 + 12f, lineColor);
            SourcePrimitiveDrawing.Rect(context, this, num3, num4, num, num2, bgColor);
            for (int i = 0; i <= 36; i++)
            {
                float num5 = (float)(73 - i * 2) * scale;
                switch (direction)
                {
                    case Direction.Down:
                        SourcePrimitiveDrawing.Rect(context, this, val2.X - num5 / 2f, num4 + num2 + (float)i, num5, 1f,
                            lineColor);
                        if (num5 > 12f)
                        {
                            SourcePrimitiveDrawing.Rect(context, this, val2.X - num5 / 2f + 6f, num4 + num2 + (float)i,
                                num5 - 12f, 1f, bgColor);
                        }

                        break;
                    case Direction.Up:
                        SourcePrimitiveDrawing.Rect(context, this, val2.X - num5 / 2f, num4 - (float)i - 1f, num5, 1f,
                            lineColor);
                        if (num5 > 12f)
                        {
                            SourcePrimitiveDrawing.Rect(context, this, val2.X - num5 / 2f + 6f, num4 - (float)i,
                                num5 - 12f, 1f, bgColor);
                        }

                        break;
                    case Direction.Left:
                        SourcePrimitiveDrawing.Rect(context, this, num3 - (float)i - 1f, num4 + num2 / 2f - num5 / 2f,
                            1f, num5, lineColor);
                        if (num5 > 12f)
                        {
                            SourcePrimitiveDrawing.Rect(context, this, num3 - (float)i,
                                num4 + num2 / 2f - num5 / 2f + 6f, 1f, num5 - 12f, bgColor);
                        }

                        break;
                    case Direction.Right:
                        SourcePrimitiveDrawing.Rect(context, this, num3 + num + (float)i, num4 + num2 / 2f - num5 / 2f,
                            1f, num5, lineColor);
                        if (num5 > 12f)
                        {
                            SourcePrimitiveDrawing.Rect(context, this, num3 + num + (float)i,
                                num4 + num2 / 2f - num5 / 2f + 6f, 1f, num5 - 12f, bgColor);
                        }

                        break;
                }
            }

            if (!(num <= 3f))
            {
                fancyText.DrawJustifyPerLine(context, this, (ISourceRichTextDrawProcess)engine,
                    new Vector2(val2.X, num4 + 16f), new Vector2(0.5f, 0f), new Vector2(scale, 1f) * fancyTextScale, 1f,
                    0, int.MaxValue);
            }
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            foreach (CustomBirdTutorialTrigger trigger in ((ISourceScene)scene).SourceScene.Entities.GetTrackedEntities(
                         typeof(CustomBirdTutorialTrigger)))
                if (trigger.ShowTutorial && trigger.BirdId == birdId)
                    return;
            TriggerShowTutorial();
        }

        public static FancyTextTutorial FindById(OriginalLevelSceneRoot level, string birdId)
        {
            foreach (FancyTextTutorial bird in level.SourceScene.Entities.GetTrackedEntities(typeof(FancyTextTutorial)))
                if (bird.birdId == birdId)
                    return bird;
            return null;
        }

        public void TriggerShowTutorial()
        {
            if (!triggered)
            {
                triggered = true;
                open = true;
            }
        }

        public void TriggerHideTutorial()
        {
            if (open)
            {
                open = false;
                if (onlyOnce) ((OriginalLevelSceneRoot)Scene).Session.DoNotLoad.Add(entityID);
            }
        }
    }
}
