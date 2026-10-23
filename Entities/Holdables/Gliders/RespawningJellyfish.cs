using System.Collections;
using System.Numerics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Graphics;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Gameplay.Holdables.Gliders;
using Celeste.Game.Presentation.Particles.Definitions.Glider;
using Celeste.Graphics.Content;
using Celeste.Mods.MaxHelpingHand.Runtime.Holdables.Gliders;
using Celeste.Physics.Collision;
using Celeste.Scene.Entities;
using Celeste.Simulation;

namespace Celeste.Mods.MaxHelpingHand.Entities.Holdables.Gliders
{
    [CustomEntity("MaxHelpingHand/RespawningJellyfish")]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.RespawningJellyfish")]
    public class RespawningJellyfish : OriginalGliderState
    {
        private readonly ISourceEngineContext engine;
        private readonly RespawningJellyfishParticles particles;
        private readonly GliderParticleDefinitions particleDefinitions;
        private readonly float respawnTime;
        private readonly bool respawnBubble;
        private readonly Vector2 initialPosition;
        private bool respawning;
        private bool shouldRespawn = true;

        public RespawningJellyfish(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(data, offset, engine)
        {
            this.engine = engine;
            particles = RespawningJellyfishParticles.For(engine);
            particleDefinitions = ((IOriginalGameplayProcess)engine).ParticleTypes.Glider;
            respawnTime = data.Float("respawnTime");
            respawnBubble = data.Bool("bubble") || data.Bool("platform");
            initialPosition = Position;
            var sprite = GliderSprite;
            sprite.SetFrameSource(((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas);
            sprite.Path = data.Attr("spriteDirectory", "objects/MaxHelpingHand/glider") + "/";
            sprite.Stop();
            sprite.ClearAnimations();
            sprite.AddLoop("idle", "idle", .1f);
            sprite.AddLoop("held", "held", .1f);
            sprite.Add("fall", "fall", .06f, "fallLoop");
            sprite.AddLoop("fallLoop", "fallLoop", .06f);
            sprite.Add("death", "death", .06f);
            sprite.Add("respawn", "respawn", .03f, "idle");
            sprite.Play("idle");
            sprite.OnFinish += animation =>
            {
                if (animation == "death") Visible = false;
            };
            Add(new RoomTransitionListener { OnOutBegin = () => shouldRespawn = false });
        }

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new RespawningJellyfish(level.EngineContext, data, offset);

        public override void Update()
        {
            if (shouldRespawn && !respawning && Top + Speed.Y * engine.DeltaTime >
                ((OriginalLevelSceneRoot)Scene).Bounds.Bottom + 16)
                RemoveAndRespawn();
            var definitions = ((IOriginalGameplayProcess)engine).ParticleTypes.Glider;
            var glow = particleDefinitions.P_Glow;
            if (!Visible) particleDefinitions.P_Glow = particles.NotGlow;
            base.Update();
            particleDefinitions.P_Glow = glow;
        }

        protected override void OnSquish(PixelCollision collision)
        {
            if (shouldRespawn)
            {
                if (!TrySquishWiggle(collision)) RemoveAndRespawn();
            }
            else base.OnSquish(collision);
        }

        private void RemoveAndRespawn()
        {
            Collidable = false;
            Visible = false;
            Add(new SimulationCoroutine(engine, RespawnRoutine()));
            GliderDestroyed = true;
        }

        internal IEnumerator DestroyThenRespawnRoutine()
        {
            var gameplay = (IOriginalGameplayProcess)engine;
            gameplay.AudioSession.Play("event:/new_content/game/10_farewell/glider_emancipate", Position,
                gameplay.SaveContext);
            GliderSprite.Play("death");
            return RespawnRoutine();
        }

        private IEnumerator RespawnRoutine()
        {
            respawning = true;
            yield return respawnTime;
            Visible = true;
            Position = initialPosition;
            Speed = Vector2.Zero;
            GliderSprite.Play("respawn");
            yield return .24f;
            respawning = false;
            Collidable = true;
            GliderDestroyed = false;
            GliderBubble = respawnBubble;
        }
    }
}
