using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using Celeste.Core.Components;
using Celeste.Core.Components.Animation;
using Celeste.Core.Components.Timing;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Entities.Triggers.Lava;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Startup;
using Celeste.Game.Gameplay.Environment.Lava;
using Celeste.Game.Entities.Protocols;
using Celeste.Graphics.Display.Colors;
using Celeste.Mods.MaxHelpingHand.Components.Lava;
using Celeste.Mods.MaxHelpingHand.Runtime.Lava;
using Celeste.Mods.MaxHelpingHand.Triggers.Lava;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation.Gameplay.Environment.Core;
using Celeste.Simulation.Gameplay.Player.Collision.Components;

namespace Celeste.Mods.MaxHelpingHand.Entities.Lava
{
    [CustomEntity("MaxHelpingHand/SidewaysLava=Load")]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.SidewaysLava")]
    public class SidewaysLava : SceneEntityState
    {
        [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.SidewaysLava+LavaMode")]
        private enum LavaMode
        {
            LeftToRight,
            RightToLeft,
            Sandwich
        }

        private static readonly Func<EverestLavaBlockerTriggerState, bool> lavaBlockerTriggerEnabled =
            CreateEnabledReader();

        private readonly IOriginalLavaProcess engine;
        private readonly SidewaysLavaCamera cameras;

        private const float Speed = 30f;

        private List<EverestLavaBlockerTriggerState> lavaBlockerTriggers;

        private bool intro;

        private LavaMode lavaMode;

        private float speedMultiplier;

        private PackedColor[] hotColors;

        private PackedColor[] coldColors;

        private string sound;

        private RoomCoreMode forcedCoreMode;

        private string flag;

        private bool iceMode;

        private bool waiting;

        private float delay;

        private float lerp;

        private bool sandwichLeaving;

        private float sandwichTransitionStartX;

        private bool sandwichHasToSetPosition;

        private bool sandwichTransferred;

        private SidewaysLavaRect leftRect;

        private SidewaysLavaRect rightRect;

        private int lastCameraWidth;

        private int lastCameraHeight;

        private SoundSourceComponentState loopSfx;

        public SidewaysLava(IOriginalLavaProcess engine, bool intro, string lavaMode, float speedMultiplier)
            : this(engine, new OriginalLevelEntityData
            {
                Values = new Dictionary<string, object>
                {
                    { "intro", intro },
                    { "lavaMode", lavaMode },
                    { "speedMultiplier", speedMultiplier }
                }
            }, Vector2.Zero)
        {
        }


        public SidewaysLava(IOriginalLavaProcess engine, OriginalLevelEntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
            this.engine = engine;
            cameras = SidewaysLavaCamera.For(engine);
            intro = data.Bool("intro", false);
            lavaMode = data.Enum<LavaMode>("lavaMode", LavaMode.LeftToRight);
            speedMultiplier = data.Float("speedMultiplier", 1f);
            sound = data.Attr("sound", "event:/game/09_core/rising_threat");
            forcedCoreMode = data.Enum<RoomCoreMode>("forceCoreMode", (RoomCoreMode)0);
            flag = data.Attr("flag", "");
            hotColors = new PackedColor[3];
            hotColors[0] = SourceHexColor.HexToColor(data.Attr("hotSurfaceColor", "ff8933"));
            hotColors[1] = SourceHexColor.HexToColor(data.Attr("hotEdgeColor", "f25e29"));
            hotColors[2] = SourceHexColor.HexToColor(data.Attr("hotCenterColor", "d01c01"));
            coldColors = new PackedColor[3];
            coldColors[0] = SourceHexColor.HexToColor(data.Attr("coldSurfaceColor", "33ffe7"));
            coldColors[1] = SourceHexColor.HexToColor(data.Attr("coldEdgeColor", "4ca2eb"));
            coldColors[2] = SourceHexColor.HexToColor(data.Attr("coldCenterColor", "0151d0"));
            this.Depth = -1000000;
            setupCollider();
            base.Visible = false;
            Add(new PlayerColliderComponentState(OnPlayer));
            Add(new CoreModeListenerState(OnChangeMode));
            SoundSourceComponentState val = engine.SoundSources.Create();
            SoundSourceComponentState val2 = val;
            loopSfx = val;
            Add(val2);
            float num = (float)cameras.Width + speedMultiplier * 80f;
            if (lavaMode != LavaMode.RightToLeft)
            {
                Add(leftRect = new SidewaysLavaRect(num, (float)cameras.Height + 20f, 4,
                    SidewaysLavaRect.OnlyModes.OnlyLeft, engine));
                leftRect.Position = new Vector2(0f - num, 0f);
                leftRect.SmallWaveAmplitude = 2f;
            }

            if (lavaMode != LavaMode.LeftToRight)
            {
                Add(rightRect = new SidewaysLavaRect(num, (float)cameras.Height + 20f, 4,
                    SidewaysLavaRect.OnlyModes.OnlyRight, engine));
                rightRect.Position =
                    new Vector2((lavaMode == LavaMode.Sandwich) ? ((float)cameras.Width - 40f) : ((float)cameras.Width),
                        0f);
                rightRect.SmallWaveAmplitude = 2f;
            }

            if (lavaMode == LavaMode.Sandwich)
            {
                Add(new RoomTransitionListener
                {
                    OnOutBegin = delegate
                    {
                        sandwichTransitionStartX = this.X;
                        if (!sandwichTransferred)
                        {
                            Tags |= ((IOriginalGameTagsEnvironment)engine).Tags.TransitionUpdate.Value;
                            sandwichLeaving = true;
                            base.Collidable = false;
                            SourceAlarmState.Set(engine.AlarmPool, OwnedComponents, 2f,
                                delegate { this.RemoveSelf(); });
                        }
                        else
                        {
                            sandwichTransferred = false;
                            lavaBlockerTriggers = ((ISourceScene)Scene).SourceScene.Entities
                                .OfType<EverestLavaBlockerTriggerState>().ToList();
                        }
                    },
                    OnOut = delegate(float progress)
                    {
                        if (this.Scene != null)
                        {
                            var scene = Scene;
                            var val3 = scene as OriginalLevelSceneRoot;
                            this.Y = val3.Camera.Y - 10f;
                            if (!sandwichLeaving)
                            {
                                this.X = Lerp(sandwichTransitionStartX, val3.Camera.Left + 20f, progress);
                            }
                        }

                        if (progress > 0.95f && sandwichLeaving)
                        {
                            this.RemoveSelf();
                        }
                    }
                });
            }

            lastCameraWidth = cameras.Width;
            lastCameraHeight = cameras.Height;
        }

        public static SidewaysLava Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new((IOriginalLavaProcess)level.EngineContext, data, offset);

        private float Approach(float value, float target, float amount) =>
            ScalarMath.Approach(value, target, amount, engine.ExtremaArithmetic);

        private float Lerp(float a, float b, float p) => ScalarMath.Lerp(a, b, p, engine.ScalarArithmetic);

        private static Func<EverestLavaBlockerTriggerState, bool> CreateEnabledReader()
        {
            var type = typeof(EverestLavaBlockerTriggerState);
            var method = new DynamicMethod("ReadLavaBlockerEnabled", typeof(bool), new[] { type }, type, true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, type.GetField("enabled", BindingFlags.Instance | BindingFlags.NonPublic));
            il.Emit(OpCodes.Ret);
            return (Func<EverestLavaBlockerTriggerState, bool>)method.CreateDelegate(
                typeof(Func<EverestLavaBlockerTriggerState, bool>));
        }

        private EverestLavaBlockerTriggerState FindBlocker(RoomPlayerSession player)
        {
            for (int i = 0; i < lavaBlockerTriggers.Count; i++)
                if (SourceEntityCollision.Check(player, lavaBlockerTriggers[i]))
                    return lavaBlockerTriggers[i];
            return null;
        }

        private bool CollidesInstantBlocker(RoomPlayerSession player)
        {
            var list = ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(typeof(InstantLavaBlockerTrigger));
            for (int i = 0; i < list.Count; i++)
                if (SourceEntityCollision.Check(player, (InstantLavaBlockerTrigger)list[i]))
                    return true;
            return false;
        }

        private void setupCollider()
        {
            if (lavaMode == LavaMode.LeftToRight)
            {
                this.Collider = new HitboxColliderGeometry((float)cameras.Width + 20f, (float)cameras.Height + 20f,
                    (float)(-cameras.Width) - 20f, 0f);
                return;
            }

            if (lavaMode == LavaMode.RightToLeft)
            {
                this.Collider = new HitboxColliderGeometry((float)cameras.Width + 20f, (float)cameras.Height + 20f,
                    (float)cameras.Width, 0f);
                return;
            }

            this.Collider = new ColliderListGeometry(new ColliderGeometry[2]
            {
                new HitboxColliderGeometry((float)cameras.Width + 20f, (float)cameras.Height + 20f,
                    (float)(-cameras.Width) - 20f, 0f),
                new HitboxColliderGeometry((float)cameras.Width + 20f, (float)cameras.Height + 20f,
                    (float)cameras.Width - 40f, 0f)
            });
        }

        private void checkCameraDimensionChange()
        {
            if (cameras.Width != lastCameraWidth || cameras.Height != lastCameraHeight)
            {
                setupCollider();
                float num = (float)cameras.Width + speedMultiplier * 80f;
                if (lavaMode != LavaMode.RightToLeft)
                {
                    leftRect.Resize(num, (float)cameras.Height + 20f, leftRect.SurfaceStep);
                    leftRect.Position = new Vector2(0f - num, 0f);
                }

                if (lavaMode != LavaMode.LeftToRight)
                {
                    rightRect.Resize(num, (float)cameras.Height + 20f, rightRect.SurfaceStep);
                    rightRect.Position =
                        new Vector2(
                            (lavaMode == LavaMode.Sandwich) ? ((float)cameras.Width - 40f) : ((float)cameras.Width),
                            0f);
                }

                if (lavaMode == LavaMode.RightToLeft || (lavaMode == LavaMode.Sandwich && iceMode))
                {
                    this.X = this.X - (float)(cameras.Width - lastCameraWidth);
                }

                lastCameraWidth = cameras.Width;
                lastCameraHeight = cameras.Height;
            }
        }

        private static PackedColor[] parseColors(string input)
        {
            string[] array = input.Split(',');
            PackedColor[] array2 = new PackedColor[array.Length];
            for (int i = 0; i < array2.Length; i++)
            {
                array2[i] = SourceHexColor.HexToColor(array[i]);
            }

            return array2;
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            iceMode = (int)getActiveCoreMode(((OriginalLevelSceneRoot)Scene).CoreMode) == 2;
            loopSfx.Play(sound, "room_state", (float)(iceMode ? 1 : 0));
            IntRect bounds;
            if (lavaMode == LavaMode.LeftToRight)
            {
                bounds = ((OriginalLevelSceneRoot)Scene).Bounds;
                this.X = bounds.Left - 16;
                loopSfx.Position = new Vector2(0f, this.Height / 2f);
            }
            else if (lavaMode == LavaMode.RightToLeft)
            {
                bounds = ((OriginalLevelSceneRoot)Scene).Bounds;
                this.X = bounds.Right - cameras.Width + 16;
                loopSfx.Position = new Vector2((float)cameras.Width, this.Height / 2f);
            }
            else
            {
                sandwichHasToSetPosition = true;
                loopSfx.Position = new Vector2(140f, this.Height / 2f);
            }

            bounds = ((OriginalLevelSceneRoot)Scene).Bounds;
            this.Y = bounds.Top - 10;
            if (lavaMode != LavaMode.Sandwich)
            {
                return;
            }

            List<SidewaysLava> list = new List<SidewaysLava>(
                from lava in ((ISourceScene)Scene).SourceScene.Entities.FindAll<SidewaysLava>()
                where lava.lavaMode == LavaMode.Sandwich
                select lava);
            bool flag = false;
            if (list.Count >= 2)
            {
                SidewaysLava sidewaysLava = ((list[0] == this) ? list[1] : list[0]);
                if (!sidewaysLava.sandwichLeaving)
                {
                    sidewaysLava.speedMultiplier = speedMultiplier;
                    sidewaysLava.sandwichTransferred = true;
                    this.RemoveSelf();
                    flag = true;
                }
            }

            if (!flag)
            {
                Tags = ((IOriginalGameTagsEnvironment)engine).Tags.Persistent.Value;
                if ((int)((OriginalLevelSceneRoot)scene).LastIntroType != 1)
                {
                    leftRect.Position.X -= 60f;
                    rightRect.Position.X += 60f;
                }
                else
                {
                    base.Visible = true;
                }
            }
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            if (intro) waiting = true;
            else
            {
                var player = ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>();
                if (player != null && player.JustRespawned) waiting = true;
            }

            if (intro)
            {
                base.Visible = true;
            }

            lavaBlockerTriggers = ((ISourceScene)scene).SourceScene.Entities.OfType<EverestLavaBlockerTriggerState>()
                .ToList();
        }

        private void OnChangeMode(RoomCoreMode mode)
        {
            iceMode = (int)getActiveCoreMode(mode) == 2;
            loopSfx.Param("room_state", (float)(iceMode ? 1 : 0));
        }

        private RoomCoreMode getActiveCoreMode(RoomCoreMode actualCoreMode)
        {
            if ((int)forcedCoreMode == 0)
            {
                return actualCoreMode;
            }

            return forcedCoreMode;
        }

        private void OnPlayer(RoomPlayerSession player)
        {
            int num = ((lavaMode == LavaMode.LeftToRight)
                ? (-1)
                : ((lavaMode == LavaMode.RightToLeft)
                    ? 1
                    : ((player.X > this.X + rightRect.Position.X - 32f) ? 1 : (-1))));
            if (engine.SaveContext.CurrentSave.Assists.Invincible)
            {
                if (delay <= 0f)
                {
                    float from = this.X;
                    float to = this.X + 48f * (float)num;
                    player.Data.Motion.Normal.Speed.X = -200f * (float)num;
                    player.RefillDash();
                    SourceTweenState.Set(engine.TweenPool, OwnedComponents, SourceTweenState.TweenMode.Oneshot, 0.4f,
                        engine.EasingArithmetic.CubeOut,
                        delegate(SourceTweenState t) { this.X = Lerp(from, to, t.Eased); });
                    delay = 0.5f;
                    loopSfx.Param("rising", 0f);
                    engine.SpatialAudio.Play("event:/game/general/assist_screenbottom", player.Position);
                }
            }
            else
            {
                player.Die(Vector2.UnitX * (float)(-num), false, true);
            }
        }

        public override void Update()
        {
            checkCameraDimensionChange();
            if (sandwichHasToSetPosition)
            {
                sandwichHasToSetPosition = false;
                this.X = ((OriginalLevelSceneRoot)Scene).Camera.Left + 20f;
            }

            delay -= engine.DeltaTime;
            this.Y = ((OriginalLevelSceneRoot)Scene).Camera.Y - 10f;
            base.Update();
            base.Visible = true;
            RoomPlayerSession player = ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>();
            if (player != null)
            {
                EverestLavaBlockerTriggerState val = FindBlocker(player);
                if (val != null && lavaBlockerTriggerEnabled(val))
                {
                    waiting = true;
                }
            }

            if (!string.IsNullOrEmpty(flag) && !((OriginalLevelSceneRoot)Scene).Session.GetFlag(flag))
            {
                waiting = true;
            }

            if (waiting)
            {
                loopSfx.Param("rising", 0f);
                if (player == null || !player.JustRespawned)
                {
                    waiting = false;
                }
                else if (lavaMode != LavaMode.Sandwich)
                {
                    float num = ((lavaMode != LavaMode.LeftToRight)
                        ? (player.X - (float)cameras.Width + 32f)
                        : (player.X - 32f));
                    if (!intro && player != null && player.JustRespawned && !CollidesInstantBlocker(player))
                    {
                        this.X = Approach(this.X, num, 32f * speedMultiplier * engine.DeltaTime);
                    }
                }
            }
            else if (lavaMode != LavaMode.Sandwich)
            {
                float num3;
                int num4;
                float num2;
                if (lavaMode == LavaMode.LeftToRight)
                {
                    num2 = ((OriginalLevelSceneRoot)Scene).Camera.Left + 21f;
                    if (this.Right < num2 - 96f)
                    {
                        this.Right = num2 - 96f;
                    }

                    num3 = this.Right;
                    num4 = 1;
                }
                else
                {
                    num2 = ((OriginalLevelSceneRoot)Scene).Camera.Right - 21f;
                    if (this.Left > num2 + 96f)
                    {
                        this.Left = num2 + 96f;
                    }

                    num2 *= -1f;
                    num3 = 0f - this.Left;
                    num4 = -1;
                }

                float num5 = ((num3 > num2)
                    ? ScalarMath.ClampedMap(num3 - num2, 0f, 56f, 1f, 0.5f)
                    : ScalarMath.ClampedMap(num3 - num2, 0f, 170f, 1f, 2f));
                if (delay <= 0f)
                {
                    loopSfx.Param("rising", 1f);
                    this.X = this.X + 30f * num5 * speedMultiplier * (float)num4 * engine.DeltaTime;
                }
            }
            else
            {
                int num6 = ((!iceMode) ? 1 : (-1));
                loopSfx.Param("rising", 1f);
                this.X = this.X + 20f * speedMultiplier * (float)num6 * engine.DeltaTime;
            }

            lerp = Approach(lerp, (float)(iceMode ? 1 : 0), engine.DeltaTime * 4f);
            if (leftRect != null)
            {
                leftRect.SurfaceColor = PackedColor.Lerp(hotColors[0], coldColors[0], lerp);
                leftRect.EdgeColor = PackedColor.Lerp(hotColors[1], coldColors[1], lerp);
                leftRect.CenterColor = PackedColor.Lerp(hotColors[2], coldColors[2], lerp);
                leftRect.Spikey = lerp * 5f;
                leftRect.UpdateMultiplier = (1f - lerp) * 2f;
                leftRect.Fade = (iceMode ? 128 : 32);
            }

            if (rightRect != null)
            {
                rightRect.SurfaceColor = PackedColor.Lerp(hotColors[0], coldColors[0], lerp);
                rightRect.EdgeColor = PackedColor.Lerp(hotColors[1], coldColors[1], lerp);
                rightRect.CenterColor = PackedColor.Lerp(hotColors[2], coldColors[2], lerp);
                rightRect.Spikey = lerp * 5f;
                rightRect.UpdateMultiplier = (1f - lerp) * 2f;
                rightRect.Fade = (iceMode ? 128 : 32);
            }

            if (lavaMode == LavaMode.Sandwich)
            {
                leftRect.Position.X = Approach(leftRect.Position.X,
                    0f - leftRect.Width + (float)(sandwichLeaving ? (-512) : 0),
                    (float)(sandwichLeaving ? 512 : 64) * engine.DeltaTime);
                rightRect.Position.X = Approach(rightRect.Position.X,
                    (float)cameras.Width - 40f + (float)(sandwichLeaving ? 512 : 0),
                    (float)(sandwichLeaving ? 512 : 64) * engine.DeltaTime);
            }
        }
    }
}
