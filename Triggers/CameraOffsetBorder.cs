using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Scheduling;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Startup;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;
using System.Numerics;
using MonoMod.RuntimeDetour;
using System;
using System.Collections;
using System.Reflection;
using Celeste.Everest.Diagnostics.Logging;

namespace Celeste.Mods.MaxHelpingHand.Triggers
{
    // not really a trigger, but that works as well.
    [CustomEntity("MaxHelpingHand/CameraOffsetBorder")]
    [SceneTracked]
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Triggers.CameraOffsetBorder")]
    public class CameraOffsetBorder : SourceTriggerEntityState
    {
        private readonly bool topLeft,
            topCenter,
            topRight,
            centerLeft,
            centerRight,
            bottomLeft,
            bottomCenter,
            bottomRight,
            inside,
            inverted;

        private readonly string flag;

        public CameraOffsetBorder(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(
            ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
            data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            topLeft = data.Bool("topLeft");
            topCenter = data.Bool("topCenter");
            topRight = data.Bool("topRight");
            centerLeft = data.Bool("centerLeft");
            centerRight = data.Bool("centerRight");
            bottomLeft = data.Bool("bottomLeft");
            bottomCenter = data.Bool("bottomCenter");
            bottomRight = data.Bool("bottomRight");
            inside = data.Bool("inside");

            flag = data.Attr("flag");
            inverted = data.Bool("inverted");
            AddTag(((IOriginalGameTagsEnvironment)engine).Tags.TransitionUpdate.Value);
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            Update();
        }

        public override void Update()
        {
            base.Update();

            RoomPlayerSession player =
                (RoomPlayerSession)((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>();
            if (player != null)
            {
                if (!string.IsNullOrEmpty(flag) && !(Scene as OriginalLevelSceneRoot).Session.GetFlag(flag) &&
                    !inverted)
                {
                    // trigger is flag-toggled, but the associated flag is disabled and the trigger has "inverted" disabled.
                    Collidable = false;
                }
                else if (!string.IsNullOrEmpty(flag) && (Scene as OriginalLevelSceneRoot).Session.GetFlag(flag) &&
                         inverted)
                {
                    // trigger is flag-toggled, but the associated flag is enabled and the trigger has "inverted" enabled.
                    Collidable = false;
                }
                else
                {
                    bool top = player.Bottom <= Top;
                    bool centerY = player.Bottom > Top && player.Top < Bottom;
                    bool bottom = player.Top >= Bottom;
                    bool left = player.Right <= Left;
                    bool centerX = player.Right > Left && player.Left < Right;
                    bool right = player.Left >= Right;

                    Collidable =
                        (topLeft && top && left) ||
                        (topCenter && top && centerX) ||
                        (topRight && top && right) ||
                        (centerLeft && centerY && left) ||
                        (centerRight && centerY && right) ||
                        (bottomLeft && bottom && left) ||
                        (bottomCenter && bottom && centerX) ||
                        (bottomRight && bottom && right) ||
                        (inside && centerX && centerY);
                }
            }
        }

        internal static Vector2 ModifyCameraTarget(Func<RoomPlayerSession, Vector2> orig, RoomPlayerSession self)
        {
            if (self.Scene == null) return orig(self);

            Vector2 target = orig(self);

            foreach (CameraOffsetBorder border in ((ISourceScene)self.Scene).SourceScene.Entities.GetTrackedEntities(
                         typeof(CameraOffsetBorder)))
            {
                if (!border.Collidable) continue;

                while (true)
                {
                    float viewLeft = target.X;
                    float viewRight = target.X + 320;
                    float viewTop = target.Y;
                    float viewBottom = target.Y + 180;

                    // Check for overlap
                    bool overlaps = viewLeft < border.Right && viewRight > border.Left && viewTop < border.Bottom &&
                                    viewBottom > border.Top;
                    if (!overlaps) break;

                    if (self.Left <= border.Right && viewRight > border.Left &&
                        (border.topLeft || border.centerLeft || border.bottomLeft))
                    {
                        // player is on the left, camera is too far right => push camera to the left
                        target.X -= viewRight - border.Left;
                    }
                    else if (self.Right >= border.Left && viewLeft < border.Right &&
                             (border.topRight || border.centerRight || border.bottomRight))
                    {
                        // player is on the right, camera is too far left => push camera to the right
                        target.X += border.Right - viewLeft;
                    }
                    else if (self.Bottom >= border.Top && viewTop < border.Bottom &&
                             (border.bottomLeft || border.bottomCenter || border.bottomRight))
                    {
                        // player is on the bottom, camera is too far up => push camera to the bottom
                        target.Y += border.Bottom - viewTop;
                    }
                    else if (self.Top <= border.Bottom && viewBottom > border.Top &&
                             (border.topLeft || border.topCenter || border.topRight))
                    {
                        // player is on the top, camera is too far down => push camera to the top
                        target.Y -= viewBottom - border.Top;
                    }
                    else
                    {
                        ((IEverestLoggerProcessEnvironment)((OriginalLevelSceneRoot)self.Scene).EngineContext).Logger
                            .Log("MaxHelpingHand/CameraOffsetBorder",
                                "Camera offset border is on-screen but we did not find a resolving direction.");
                        break;
                    }
                }
            }

            return target;
        }
    }
}
