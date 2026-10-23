// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Components.Timing;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Randomness;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.LevelLoader;
using Celeste.Game.Gameplay.Enemies.Seekers;
using Celeste.Game.Gameplay.Mechanisms.Switches;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Display;
using Celeste.Graphics.Display.Colors;
using Celeste.Mods.MaxHelpingHand.Modules;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Components.Oscillation;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Audio.Emitters;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation.Gameplay.Holdables;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.Particles.Definitions;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;

namespace Celeste.Mods.MaxHelpingHand.Entities.Switches
{
    [CustomEntity("MaxHelpingHand/FlagTouchSwitch")]
    [SceneTracked(inherited: true)]
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.FlagTouchSwitch")]
    public class FlagTouchSwitch : SceneEntityState
    {
        private readonly IOriginalTouchSwitchProcess process;
        private readonly MaxHelpingHandModule module;
        private ParticleTypePresentation P_RecoloredFire;

        private int id;
        private string flag;
        public string Flag => flag;

        private bool inverted;
        private bool allowDisable;

        // contains all the touch switches in the room
        private List<FlagTouchSwitch> allTouchSwitchesInRoom;
        private List<Celeste.Mods.MaxHelpingHand.Contracts.IFlagMovingTouchSwitch> allMovingFlagTouchSwitchesInRoom;


        public bool Activated { get; private set; } = false;
        public bool Finished { get; private set; } = false;

        private SoundSourceComponentState touchSfx;

        private AtlasEntry border;

        protected Sprite icon;

        private int[] frames;
        private bool persistent;

        private PackedColor inactiveColor;
        private PackedColor activeColor;
        private PackedColor finishColor;

        private bool smoke;

        private float ease;

        private WigglerComponent wiggler;

        private Vector2 pulse = Vector2.One;

        private float timer = 0f;

        private BloomPointState bloom;

        private string hitSound;
        private string completeSoundFromSwitch;
        private string completeSoundFromScene;

        private string hideIfFlag;

        private OriginalLevelSceneRoot level => (OriginalLevelSceneRoot)Scene;

        protected virtual Vector2 IconPosition => Vector2.Zero;

        public FlagTouchSwitch(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            process = (IOriginalTouchSwitchProcess)engine;
            module = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<MaxHelpingHandModule>().Single();
            border = process.GameGraphics.GameAtlas["objects/touchswitch/container"];
            Depth = 2000;

            id = data.ID;
            flag = data.Attr("flag");
            persistent = data.Bool("persistent", false);

            inactiveColor = PackedColor.FromHex(data.Attr("inactiveColor", "5FCDE4"));
            activeColor = PackedColor.FromHex(data.Attr("activeColor", "FFFFFF"));
            finishColor = PackedColor.FromHex(data.Attr("finishColor", "F141DF"));

            hitSound = data.Attr("hitSound", "event:/game/general/touchswitch_any");
            completeSoundFromSwitch =
                data.Attr("completeSoundFromSwitch", "event:/game/general/touchswitch_last_cutoff");
            completeSoundFromScene =
                data.Attr("completeSoundFromScene", "event:/game/general/touchswitch_last_oneshot");

            hideIfFlag = data.Attr("hideIfFlag");

            inverted = data.Bool("inverted", false);
            allowDisable = data.Bool("allowDisable", false);

            smoke = data.Bool("smoke", true);

            string borderTexturePath = data.Attr("borderTexture");
            if (!string.IsNullOrEmpty(borderTexturePath))
            {
                border = process.GameGraphics.GameAtlas[borderTexturePath];
            }

            P_RecoloredFire = new ParticleTypePresentation(process.ParticleTypes.TouchSwitch.P_Fire)
            {
                Color = finishColor
            };

            setUpCollision(data);

            // set up the icon
            string iconAttribute = data.Attr("icon", "vanilla");

            // Try to get the number of frames in the icon animation. If it works, then make an int[] for that range, and if not, stick to the default 6 frames.
            if (int.TryParse(data.Attr("animationLength", "6"), out int frameVal))
                frames = Enumerable.Range(0, frameVal).ToArray();
            else
                frames = new int[] { 0, 1, 2, 3, 4, 5 };
            icon = new Sprite(process, process.GameGraphics.GameAtlas,
                iconAttribute == "vanilla"
                    ? "objects/touchswitch/icon"
                    : $"objects/MaxHelpingHand/flagTouchSwitch/{iconAttribute}/icon");
            Add(icon);
            icon.Add("idle", "", 0f, default(int));
            icon.Add("spin", "", 0.1f, new Chooser<string>("spin", 1f), frames);
            icon.Play("spin");
            icon.Color = inactiveColor;
            icon.CenterOrigin();
            icon.Position = IconPosition;

            Add(bloom = new BloomPointState(0f, 16f));
            bloom.Alpha = 0f;
            bloom.Position = IconPosition;

            Add(wiggler = process.WigglerPool.Create(0.5f, 4f, v => { pulse = Vector2.One * (1f + v * 0.25f); }));

            Add(new VertexLightState(process, Vector2.Zero, PackedColor.White, 0.8f, 16, 32)
                { Position = IconPosition });
            touchSfx = process.SoundSources.Create();
            touchSfx.Position = IconPosition;
            Add(touchSfx);

            var scene = engine.Frame.Scenes.CurrentScene;
            var earlyLevel = module.LoadingLevel ??
                             scene as OriginalLevelSceneRoot ?? (scene as OriginalLevelLoaderScene)?.Level;
            if (earlyLevel != null) module.FlagSwitchGroups.ResetFlagIfNecessary(earlyLevel, flag, inverted);
        }

        protected virtual void setUpCollision(EntityData data)
        {
            Collider = new HitboxColliderGeometry(16f, 16f, -8f, -8f);
            if (data.Bool("playerCanActivate", defaultValue: true))
            {
                Add(new PlayerColliderComponentState(onPlayer, null, new HitboxColliderGeometry(30f, 30f, -15f, -15f)));
            }

            Add(new HoldableColliderState(onHoldable, new HitboxColliderGeometry(20f, 20f, -10f, -10f)));
            Add(new SeekerColliderState(onSeeker, new HitboxColliderGeometry(24f, 24f, -12f, -12f)));
        }

        public override void Added(object scene)
        {
            base.Added(scene);

            if (inverted != level.Session.GetFlag(flag))
            {
                // start directly finished, since the session flag is already set (or the flag is inverted and unset).
                Activated = true;
                Finished = true;

                icon.Rate = 0.1f;
                icon.Play("idle");
                icon.Color = finishColor;
                ease = 1f;
                bloom.Alpha = 1f;
            }
            else if (level.Session.GetFlag(flag + "_switch" + id))
            {
                // only that switch is activated, not the whole group.
                Activated = true;

                icon.Rate = 4f;
                icon.Color = activeColor;
                ease = 1f;
                bloom.Alpha = 1f;
            }
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);

            // look around for other touch switches that belong to the same group (same flag).
            allTouchSwitchesInRoom = level.SourceScene.Entities.GetTrackedEntities(typeof(FlagTouchSwitch))
                .OfType<FlagTouchSwitch>().Where(touchSwitch => touchSwitch.flag == flag).ToList();
            allMovingFlagTouchSwitchesInRoom = module.FlagSwitchGroups.CaptureMovingSwitches(level, flag);
        }

        protected void onPlayer(RoomPlayerSession player)
        {
            TurnOn();
        }

        protected void onHoldable(HoldableState h)
        {
            TurnOn();
        }

        protected void onSeeker(OriginalSeekerState seeker)
        {
            if (level.InsideCamera(Position, 10f))
            {
                TurnOn();
            }
        }

        public void TurnOn()
        {
            if (!Activated)
            {
                doEffect(() => touchSfx.Play(hitSound));

                Activated = true;

                // animation
                doEffect(() =>
                {
                    wiggler.Start();
                    for (int i = 0; i < 32; i++)
                    {
                        float num = SourceRandomMath.NextFloat(process.Random, (float)Math.PI * 2f);
                        level.Particles.Emit(process.ParticleTypes.TouchSwitch.P_FireWhite,
                            Position + IconPosition + Vector2Geometry.AngleToVector(num, 6f), num);
                    }
                });
                icon.Rate = 4f;

                CompleteGroup(() => doEffect(() =>
                {
                    SoundEmitterState.Play(completeSoundFromScene, process);
                    var cutoff = process.SoundSources.Create();
                    cutoff.Play(completeSoundFromSwitch);
                    cutoff.Position = IconPosition;
                    Add(cutoff);
                }));
            }
        }

        private bool CompleteGroup(Action onFinished) => module.FlagSwitchGroups.Complete(level, flag, inverted,
            persistent,
            id, allTouchSwitchesInRoom, allMovingFlagTouchSwitchesInRoom, onFinished);

        internal void FinishGroup() => finish();
        internal bool Hidden => isHidden();

        private void finish()
        {
            Finished = true;
            ease = 0f;
        }

        public override void Update()
        {
            if (isHidden())
            {
                // disable the entity entirely, and spawn another entity to continue monitoring the flag
                Visible = Active = Collidable = false;
                level.SourceScene.AddEntity(
                    new ResurrectOnFlagDisableController((ISourceEngineContext)process, this, hideIfFlag));
                return;
            }

            timer += process.DeltaTime * 8f;
            ease = ScalarMath.Approach(ease, (Finished || Activated) ? 1f : 0f, process.DeltaTime * 2f,
                process.ExtremaArithmetic);

            icon.Color = SourceColorInterpolation.Lerp(inactiveColor, Finished ? finishColor : activeColor, ease,
                process.ScalarArithmetic);
            icon.Color *= 0.5f + ((float)Math.Sin(timer) + 1f) / 2f * (1f - ease) * 0.5f + 0.5f * ease;

            bloom.Alpha = ease;
            if (Finished)
            {
                if (icon.Rate > 0.1f)
                {
                    icon.Rate -= 2f * process.DeltaTime;
                    if (icon.Rate <= 0.1f)
                    {
                        icon.Rate = 0.1f;
                        wiggler.Start();
                        icon.Play("idle");
                        level.Displacement.AddBurst(Position + IconPosition, 0.6f, 4f, 28f, 0.2f);
                    }
                }
                else if (level.SourceScene.Clock.OnInterval(0.03f, process) && smoke)
                {
                    Vector2 position = Position + IconPosition + new Vector2(0f, 1f) +
                                       Vector2Geometry.AngleToVector(
                                           SourceRandomMath.NextFloat(process.Random, (float)Math.PI * 2f), 5f);
                    level.ParticlesBG.Emit(P_RecoloredFire, position);
                }

                if (allowDisable && inverted == level.Session.GetFlag(flag))
                {
                    // we have to disable the touch switch! aaa
                    if (persistent)
                    {
                        // if the touch switch is persistent, turn its flag off.
                        level.Session.SetFlag(flag + "_switch" + id, false);
                    }

                    Activated = Finished = false;
                    icon.Rate = 1f;
                    icon.Play("spin");

                    // cancel all alarms (deferred animations).
                    foreach (SourceAlarmState alarm in Components.OfType<SourceAlarmState>().ToList())
                    {
                        alarm.RemoveSelf();
                    }
                }
            }

            base.Update();
        }

        public override void Render(ISourceRenderContext context)
        {
            renderBorder(context);
            base.Render(context);
        }

        protected virtual void renderBorder(ISourceRenderContext context)
        {
            var sink = (ISourceRenderCommandSink<AtlasTextureDraw>)context;
            var origin = new Vector2(border.FrameWidth / 2f, border.FrameHeight / 2f);
            var shadow = new AtlasTextureDraw(border, Position + new Vector2(0f, -1f), origin, PackedColor.Black,
                Vector2.One);
            sink.Draw(in shadow, this);
            var face = new AtlasTextureDraw(border, Position, origin, icon.Color, pulse);
            sink.Draw(in face, this);
        }

        private void doEffect(Action effect)
        {
            if (allowDisable)
            {
                // do the effect only after 0.05s, in case our touch switch gets disabled in the meantime.
                Add(SourceAlarmState.Create(((ISourceEngineContext)process).AlarmPool,
                    SourceAlarmState.AlarmMode.Oneshot,
                    effect, 0.05f, true));
            }
            else
            {
                // do the effect right now.
                effect();
            }
        }

        private bool isHidden()
        {
            return !string.IsNullOrEmpty(hideIfFlag) && level.Session.GetFlag(hideIfFlag);
        }
    }
}
