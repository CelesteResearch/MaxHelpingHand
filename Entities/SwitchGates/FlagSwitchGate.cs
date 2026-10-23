using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Components.Animation;
using Celeste.Core.Mathematics;
using Celeste.Core.Randomness;
using Celeste.Core.Rendering;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Everest.Modules.Updating;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Game.Chapters.Flow;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Areas;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Session;
using Celeste.Game.Gameplay.Mechanisms.SwitchGates;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Display.Colors;
using Celeste.Graphics.Rendering;
using Celeste.Physics.Collision;
using Celeste.Scene.Components.Oscillation;
using Celeste.Scene.Entities;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.Particles.Definitions;
using Celeste.Mods.MaxHelpingHand.Contracts;

namespace Celeste.Mods.MaxHelpingHand.Entities.SwitchGates
{
    /// <summary>
    /// A switch gate triggered by a flag touch switch.
    ///
    /// Attributes:
    /// - flag: the session flag this switch gate reacts to. Must be the same across the touch switch group.
    /// - icon: the name of the icon for the switch gate (relative to objects/MaxHelpingHand/flagSwitchGate) or "vanilla" for the default one.
    /// - persistent: enable to have the gate stay open even when you die / change rooms.
    /// - inactiveColor / activeColor / finishColor: custom colors for the touch switch.
    /// - sprite: the texture for the gate block.
    /// </summary>
    [CustomEntity("MaxHelpingHand/FlagSwitchGate")]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.FlagSwitchGate")]
    [SceneTracked]
    public class FlagSwitchGate : SourceSolidEntityState, IFlagSwitchGate
    {
        private ParticleTypePresentation P_RecoloredFire;
        private ParticleTypePresentation P_RecoloredFireBack;

        private readonly AtlasEntry[,] nineSlice = new AtlasEntry[3, 3];
        private readonly IOriginalSwitchGateProcess process;
        private OriginalLevelSceneRoot Level => (OriginalLevelSceneRoot)Scene;

        private Sprite icon;
        private Vector2 iconOffset;

        private WigglerComponent wiggler;

        private Vector2 node;

        private SoundSourceComponentState openSfx;

        public int ID { get; private set; }
        public string Flag { get; private set; }

        public bool Triggered { get; private set; }

        private readonly PackedColor inactiveColor;
        private readonly PackedColor activeColor;
        private readonly PackedColor finishColor;

        private readonly float shakeTime;
        private readonly float moveTime;
        private readonly float moveSpeed;
        private readonly bool moveEased;
        private readonly bool speedMode;
        private readonly bool moveImmediately;

        private readonly string moveSound;
        private readonly string finishedSound;

        private readonly bool smoke;
        private readonly bool particles;

        private readonly bool allowReturn;

        private readonly bool isShatter;
        private readonly string blockSpriteName;
        private readonly string debrisPath;
        private readonly bool shatterSound;

        public static FlagSwitchGate Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new FlagSwitchGate(level, data, offset);

        private FlagSwitchGate(OriginalLevelSceneRoot level, OriginalLevelEntityData data, Vector2 offset)
            : base(data.Position + offset, data.Width, data.Height, false,
                level.SolidWorldFactory.Create(data.Position + offset, false))
        {
            process = (IOriginalSwitchGateProcess)level.EngineContext;

            isShatter = data.Bool("isShatter", false);
            if (data.Nodes.Length > 0)
            {
                node = data.Nodes[0] + offset;
            }

            ID = data.ID;
            Flag = data.Attr("flag");

            inactiveColor = SourceHexColor.HexToColor(data.Attr("inactiveColor", "5FCDE4"));
            activeColor = SourceHexColor.HexToColor(data.Attr("activeColor", "FFFFFF"));
            finishColor = SourceHexColor.HexToColor(data.Attr("finishColor", "F141DF"));

            shakeTime = data.Float("shakeTime", 0.5f);
            moveTime = data.Float("moveTime", 1.8f);
            moveSpeed = data.Float("moveSpeed", 60.0f);
            moveEased = data.Bool("moveEased", true);
            speedMode = data.Bool("speedMode", false);
            moveImmediately = data.Bool("moveImmediately", false);

            moveSound = data.Attr("moveSound", "event:/game/general/touchswitch_gate_open");
            finishedSound = data.Attr("finishedSound", "event:/game/general/touchswitch_gate_finish");

            smoke = data.Bool("smoke", true);
            particles = data.Bool("particles", true);

            allowReturn = data.Bool("allowReturn", false);

            SurfaceSoundIndex = data.Int("surfaceIndex", SurfaceSoundIndex);

            P_RecoloredFire = new ParticleTypePresentation(process.ParticleTypes.TouchSwitch.P_Fire)
            {
                Color = finishColor
            };
            P_RecoloredFireBack = new ParticleTypePresentation(process.ParticleTypes.TouchSwitch.P_Fire)
            {
                Color = inactiveColor
            };

            string iconAttribute = data.Attr("icon", "vanilla");
            icon = new Sprite(process, process.GameGraphics.GameAtlas,
                iconAttribute == "vanilla"
                    ? "objects/switchgate/icon"
                    : $"objects/MaxHelpingHand/flagSwitchGate/{iconAttribute}/icon");
            OwnedComponents.Add(icon);
            icon.Add("spin", "", 0.1f, new Chooser<string>("spin"));
            icon.Play("spin");
            icon.Rate = 0f;
            icon.Color = inactiveColor;
            icon.Position = (iconOffset = new Vector2(data.Width / 2f, data.Height / 2f));
            icon.CenterOrigin();
            OwnedComponents.Add(wiggler =
                process.WigglerPool.Create(0.5f, 4f, f => { icon.Scale = Vector2.One * (1f + f); }));

            blockSpriteName = data.Attr("sprite", "block");
            var texture = process.GameGraphics.GameAtlas["objects/switchgate/" + blockSpriteName];
            for (int x = 0; x < 3; x++)
            for (int y = 0; y < 3; y++)
                nineSlice[x, y] = texture.GetSubtexture(x * 8, y * 8, 8, 8);
            shatterSound = data.Bool("shatterSound", true);

            debrisPath = data.Attr("debrisPath");
            if (string.IsNullOrEmpty(debrisPath))
            {
                switch (blockSpriteName)
                {
                    default:
                        debrisPath = "debris/VortexHelper/disintegate/1";
                        break;
                    case "mirror":
                        debrisPath = "debris/VortexHelper/disintegate/2";
                        break;
                    case "temple":
                        debrisPath = "debris/VortexHelper/disintegate/3";
                        break;
                    case "stars":
                        debrisPath = "debris/VortexHelper/disintegate/4";
                        break;
                }
            }

            OwnedComponents.Add(openSfx = process.SoundSources.Create());
            OwnedComponents.Add(new LightOccludeState(0.5f));
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            if ((Level.Session.GetFlag(Flag + "_gate" + ID) && !allowReturn) || Level.Session.GetFlag(Flag))
            {
                if (isShatter)
                {
                    RemoveSelf();
                }

                if (allowReturn)
                {
                    // watch the flag to return to the start if necessary.
                    OwnedComponents.Add(new SimulationCoroutine(process,
                        moveBackAndForthSequence(Position, node, startAtNode: true)));
                }

                MoveTo(node);
                icon.Rate = 0f;
                icon.SetAnimationFrame(0);
                icon.Color = finishColor;
            }
            else
            {
                if (isShatter)
                {
                    OwnedComponents.Add(new SimulationCoroutine(process, shatterSequence()));
                }
                else if (allowReturn)
                {
                    // go back and forth as needed.
                    OwnedComponents.Add(new SimulationCoroutine(process,
                        moveBackAndForthSequence(Position, node, startAtNode: false)));
                }
                else
                {
                    // we are only going to the node, then stopping.
                    OwnedComponents.Add(new SimulationCoroutine(process, moveSequence(node, goingBack: false)));
                }
            }
        }

        private bool InView()
        {
            var camera = Level.Camera;
            return Position.X + Width > camera.X - 16f && Position.Y + Height > camera.Y - 16f &&
                   Position.X < camera.X + 320f && Position.Y < camera.Y + 180f;
        }

        public override void Render(ISourceRenderContext context)
        {
            if (!InView()) return;
            int widthInTiles = (int)Collider.Width / 8 - 1;
            int heightInTiles = (int)Collider.Height / 8 - 1;
            for (int i = 0; i <= widthInTiles; i++)
            {
                int x = i < widthInTiles ? i == 0 ? 0 : 8 : 16;
                for (int j = 0; j <= heightInTiles; j++)
                {
                    int y = j < heightInTiles ? j == 0 ? 0 : 8 : 16;
                    SourceAtlasDrawing.Draw(context, this, nineSlice[x / 8, y / 8],
                        Position + Shake + new Vector2(i * 8, j * 8), Vector2.Zero, PackedColor.White, Vector2.One);
                }
            }

            icon.Position = iconOffset + Shake;
            icon.DrawOutline(context);
            base.Render(context);
        }

        private bool CollideSolid(Vector2 at) =>
            SourceSceneCollision.FirstPoint<ISourceEntityCollider>(Level.Scheduler.Entities, "Celeste.Solid", at) !=
            null;

        public void Trigger()
        {
            Triggered = true;
        }

        private IEnumerator moveBackAndForthSequence(Vector2 position, Vector2 node, bool startAtNode)
        {
            while (true)
            {
                if (!startAtNode)
                {
                    // go forth
                    IEnumerator forthSeq = moveSequence(node, goingBack: false);
                    while (forthSeq.MoveNext())
                    {
                        yield return forthSeq.Current;
                    }
                }

                // go back
                IEnumerator backSeq = moveSequence(position, goingBack: true);
                while (backSeq.MoveNext())
                {
                    yield return backSeq.Current;
                }

                startAtNode = false;
            }
        }

        private IEnumerator moveSequence(Vector2 node, bool goingBack)
        {
            Vector2 start = Position;

            PackedColor fromColor, toColor;

            if (!goingBack)
            {
                fromColor = inactiveColor;
                toColor = finishColor;
                while ((!Triggered || allowReturn) && !Level.Session.GetFlag(Flag))
                {
                    yield return null;
                }
            }
            else
            {
                fromColor = finishColor;
                toColor = inactiveColor;
                while (Level.Session.GetFlag(Flag))
                {
                    yield return null;
                }
            }

            if (!moveImmediately) yield return 0.1f;
            if (shouldCancelMove(goingBack)) yield break;

            // animate the icon
            openSfx.Play(moveSound);
            if (shakeTime > 0f && !moveImmediately)
            {
                StartShaking(shakeTime);
                while (icon.Rate < 1f)
                {
                    icon.Color =
                        SourceColorInterpolation.Lerp(fromColor, activeColor, icon.Rate, process.ScalarArithmetic);
                    icon.Rate += process.DeltaTime / shakeTime;
                    yield return null;
                    if (shouldCancelMove(goingBack)) yield break;
                }
            }
            else
            {
                icon.Rate = 1f;
            }

            if (!moveImmediately) yield return 0.1f;
            if (shouldCancelMove(goingBack)) yield break;

            // move the switch gate, emitting particles along the way if particles is true
            int particleAt = 0;

            SourceTweenState tween;
            if (speedMode)
            {
                tween = SourceTweenState.Create(process.TweenPool, SourceTweenState.TweenMode.Oneshot, null,
                    (float)Math.Round(((Vector2Math.Distance(start, node)) / moveSpeed), 3), start: true);
            }
            else
            {
                tween = SourceTweenState.Create(process.TweenPool, SourceTweenState.TweenMode.Oneshot,
                    moveEased ? process.EasingArithmetic.CubeOut : null, moveTime + (moveEased ? 0.2f : 0f),
                    start: true);
            }

            tween.OnUpdate = tweenArg =>
            {
                MoveTo(Vector2Math.Lerp(start, node, tweenArg.Eased, process.ScalarArithmetic));
                if (particles)
                {
                    if (Level.SourceScene.Clock.OnInterval(0.1f, process))
                    {
                        particleAt++;
                        particleAt %= 2;
                        for (int tileX = 0; tileX < Width / 8f; tileX++)
                        {
                            for (int tileY = 0; tileY < Height / 8f; tileY++)
                            {
                                if ((tileX + tileY) % 2 == particleAt)
                                {
                                    Level.ParticlesBG.Emit(process.ParticleTypes.SwitchGate.P_Behind,
                                        Position + new Vector2(tileX * 8, tileY * 8) +
                                        SourceRandomMath.Range(process.Random, Vector2.One * 2f, Vector2.One * 6f));
                                }
                            }
                        }
                    }
                }
            };
            OwnedComponents.Add(tween);

            float moveTimeLeft =
                speedMode ? (float)Math.Round(((Vector2Math.Distance(start, node)) / moveSpeed), 3) : moveTime;
            while (moveTimeLeft > 0f)
            {
                yield return null;
                moveTimeLeft -= process.DeltaTime;
                if (shouldCancelMove(goingBack, tween)) yield break;
            }

            bool collidableBackup = Collidable;
            Collidable = false;

            // collide dust particles on the left
            if (node.X <= start.X)
            {
                Vector2 add = new Vector2(0f, 2f);
                for (int tileY = 0; tileY < Height / 8f; tileY++)
                {
                    Vector2 collideAt = new Vector2(Left - 1f, Top + 4f + (tileY * 8));
                    Vector2 noCollideAt = collideAt + Vector2.UnitX;
                    if (CollideSolid(collideAt) && !CollideSolid(noCollideAt))
                    {
                        Level.ParticlesFG.Emit(process.ParticleTypes.SwitchGate.P_Dust, collideAt + add,
                            (float)Math.PI);
                        Level.ParticlesFG.Emit(process.ParticleTypes.SwitchGate.P_Dust, collideAt - add,
                            (float)Math.PI);
                    }
                }
            }

            // collide dust particles on the rigth
            if (node.X >= start.X)
            {
                Vector2 add = new Vector2(0f, 2f);
                for (int tileY = 0; tileY < Height / 8f; tileY++)
                {
                    Vector2 collideAt = new Vector2(Right + 1f, Top + 4f + (tileY * 8));
                    Vector2 noCollideAt = collideAt - Vector2.UnitX * 2f;
                    if (CollideSolid(collideAt) && !CollideSolid(noCollideAt))
                    {
                        Level.ParticlesFG.Emit(process.ParticleTypes.SwitchGate.P_Dust, collideAt + add, 0f);
                        Level.ParticlesFG.Emit(process.ParticleTypes.SwitchGate.P_Dust, collideAt - add, 0f);
                    }
                }
            }

            // collide dust particles on the top
            if (node.Y <= start.Y)
            {
                Vector2 add = new Vector2(2f, 0f);
                for (int tileX = 0; tileX < Width / 8f; tileX++)
                {
                    Vector2 collideAt = new Vector2(Left + 4f + (tileX * 8), Top - 1f);
                    Vector2 noCollideAt = collideAt + Vector2.UnitY;
                    if (CollideSolid(collideAt) && !CollideSolid(noCollideAt))
                    {
                        Level.ParticlesFG.Emit(process.ParticleTypes.SwitchGate.P_Dust, collideAt + add,
                            -(float)Math.PI / 2f);
                        Level.ParticlesFG.Emit(process.ParticleTypes.SwitchGate.P_Dust, collideAt - add,
                            -(float)Math.PI / 2f);
                    }
                }
            }

            // collide dust particles on the bottom
            if (node.Y >= start.Y)
            {
                Vector2 add = new Vector2(2f, 0f);
                for (int tileX = 0; tileX < Width / 8f; tileX++)
                {
                    Vector2 collideAt = new Vector2(Left + 4f + (tileX * 8), Bottom + 1f);
                    Vector2 noCollideAt = collideAt - Vector2.UnitY * 2f;
                    if (CollideSolid(collideAt) && !CollideSolid(noCollideAt))
                    {
                        Level.ParticlesFG.Emit(process.ParticleTypes.SwitchGate.P_Dust, collideAt + add,
                            (float)Math.PI / 2f);
                        Level.ParticlesFG.Emit(process.ParticleTypes.SwitchGate.P_Dust, collideAt - add,
                            (float)Math.PI / 2f);
                    }
                }
            }

            Collidable = collidableBackup;

            // moving is over
            process.AudioSession.Play(finishedSound, Position, process.SaveContext);
            StartShaking(0.2f);
            while (icon.Rate > 0f)
            {
                icon.Color =
                    SourceColorInterpolation.Lerp(activeColor, toColor, 1f - icon.Rate, process.ScalarArithmetic);
                icon.Rate -= process.DeltaTime * 4f;
                yield return null;
                if (shouldCancelMove(goingBack)) yield break;
            }

            icon.Rate = 0f;
            icon.SetAnimationFrame(0);
            wiggler.Start();

            // emit fire particles if the block is not behind a solid.
            collidableBackup = Collidable;
            Collidable = false;
            if (!CollideSolid(Center) && smoke)
            {
                for (int i = 0; i < 32; i++)
                {
                    float angle = SourceRandomMath.NextFloat(process.Random, (float)Math.PI * 2f);
                    Level.ParticlesFG.Emit(goingBack ? P_RecoloredFireBack : P_RecoloredFire,
                        Position + iconOffset + Vector2Geometry.AngleToVector(angle, 4f), angle);
                }
            }

            Collidable = collidableBackup;
        }

        private bool shouldCancelMove(bool goingBack, SourceTweenState tween = null)
        {
            if (allowReturn && Level.Session.GetFlag(Flag) == goingBack)
            {
                // whoops, the flag changed too fast! we need to backtrack.
                if (tween != null)
                {
                    Remove(tween);
                }

                icon.Rate = 0f;
                icon.SetAnimationFrame(0);
                return true;
            }

            return false;
        }

        // this is heavily inspired by Vortex Helper by catapillie, and also requires Vortex Helper to fully work.
        private IEnumerator shatterSequence()
        {
            if (!((IEverestLoaderProcessEnvironment)process).Loader.DependencyLoaded(new EverestModuleMetadata
                    { Name = "VortexHelper", Version = new Version(1, 1, 0) }))
            {
                // error postcards are nicer than crashes!
                process.AudioSession.SetMusic(null);
                ((IOriginalLevelEnterStaticEnvironment)process).LevelEnterStatics.ErrorMessage =
                    "{big}Oops!{/big}{n}To use {# F94A4A}Shatter Flag Switch Gates{#}, you need to have {# d678db}Vortex Helper{#} installed!";
                var enter = (IOriginalLevelEnterProcess)process;
                var area = new OriginalAreaKey
                {
                    ID = Level.Session.AreaID, Mode = Level.Session.Mode, SID = Level.Session.AreaSID,
                    SIDID = Level.Session.AreaSIDID
                };
                enter.LevelEnterPolicy.Go(((IOriginalSessionEnvironment)process).Sessions.CreateSession(area), false,
                    enter);
                yield break;
            }

            OriginalLevelSceneRoot level = Level;
            while ((!Triggered || allowReturn) && !Level.Session.GetFlag(Flag))
            {
                yield return null;
            }

            if (shatterSound) openSfx.Play("event:/game/general/fallblock_shake");
            yield return 0.1f;

            if (shakeTime > 0f)
            {
                StartShaking(shakeTime);
                while (icon.Rate < 1f)
                {
                    icon.Color = SourceColorInterpolation.Lerp(inactiveColor, finishColor, icon.Rate,
                        process.ScalarArithmetic);
                    icon.Rate += process.DeltaTime / shakeTime;
                    yield return null;
                }
            }
            else
            {
                icon.Rate = 1f;
            }

            yield return 0.1f;
            for (int k = 0; k < 32; k++)
            {
                float num = SourceRandomMath.NextFloat(process.Random, (float)Math.PI * 2f);
                Level.ParticlesFG.Emit(P_RecoloredFire, Position + iconOffset + Vector2Geometry.AngleToVector(num, 4f),
                    num);
            }

            openSfx.Stop();
            if (shatterSound)
                process.AudioSession.Play("event:/game/general/wall_break_stone", Center, process.SaveContext);
            process.AudioSession.Play(finishedSound, Center, process.SaveContext);
            level.Shake();

            for (int i = 0; i < Width / 8f; i++)
            {
                for (int j = 0; j < Height / 8f; j++)
                {
                    var debris = new FlagGateDebris(process, SourceWorld.TargetMotionPolicy);
                    debris.Init(Position + new Vector2(4 + i * 8, 4 + j * 8), '1').BlastFrom(Center);
                    debris.SetTexture(process.GameGraphics.GameAtlas[debrisPath]);
                    Level.SourceScene.QueueSourceEntity(debris);
                }
            }

            DestroyStaticMovers();
            RemoveSelf();
        }
    }
}
