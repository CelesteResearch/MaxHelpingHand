using System;
using System.Numerics;
using Celeste.Core.Mathematics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Collectibles.Refills;
using Celeste.Graphics.Display.Colors;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.MaxHelpingHand.Entities.Refills
{
    [CustomEntity("MaxHelpingHand/CustomizableRefill = Create")]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.CustomizableRefill")]
    public class CustomizableRefill : OriginalRefillState
    {
        private float rotation;

        public float rotationDegrees
        {
            get => rotation;
            set
            {
                rotation = value;
                outline.Rotation = sprite.Rotation = flash.Rotation = rotation * 0.017453292f;
            }
        }

        internal IOriginalRefillProcess SourceEngine => engine;

        public static CustomizableRefill Create(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) =>
            new((IOriginalRefillProcess)level.EngineContext, data, offset);

        public CustomizableRefill(IOriginalRefillProcess engine, EntityData data, Vector2 offset) : base(data, offset,
            engine)
        {
            float respawnTime = data.Float("respawnTime", 2.5f);
            var collider = Get<PlayerColliderComponentState>();
            var original = collider.OnCollide;
            collider.OnCollide = player =>
            {
                original(player);
                if (respawnTimer == 2.5f) respawnTimer = respawnTime;
            };

            if (!string.IsNullOrEmpty(data.Attr("sprite")))
            {
                string spritePath = "objects/MaxHelpingHand/refill/" + data.Attr("sprite");
                outline.Texture = engine.GameGraphics.GameAtlas[spritePath + "/outline"];
                sprite.Path = spritePath + "/idle";
                sprite.Stop();
                sprite.ClearAnimations();
                sprite.AddLoop("idle", "", 0.1f);
                sprite.Play("idle");
                flash.Path = spritePath + "/flash";
                flash.ClearAnimations();
                flash.Add("flash", "", 0.05f);
            }

            if (!string.IsNullOrEmpty(data.Attr("shatterParticleColor1")) &&
                !string.IsNullOrEmpty(data.Attr("shatterParticleColor2")))
            {
                p_shatter = new ParticleTypePresentation(engine.RefillParticles.P_Shatter)
                {
                    Color = SourceHexColor.HexToColor(data.Attr("shatterParticleColor1")),
                    Color2 = SourceHexColor.HexToColor(data.Attr("shatterParticleColor2"))
                };
            }

            if (!string.IsNullOrEmpty(data.Attr("glowParticleColor1")) &&
                !string.IsNullOrEmpty(data.Attr("glowParticleColor2")))
            {
                p_glow = new ParticleTypePresentation(engine.RefillParticles.P_Glow)
                {
                    Color = SourceHexColor.HexToColor(data.Attr("glowParticleColor1")),
                    Color2 = SourceHexColor.HexToColor(data.Attr("glowParticleColor2"))
                };
                p_regen = new ParticleTypePresentation(engine.RefillParticles.P_Regen)
                {
                    Color = SourceHexColor.HexToColor(data.Attr("glowParticleColor1")),
                    Color2 = SourceHexColor.HexToColor(data.Attr("glowParticleColor2"))
                };
            }

            rotationDegrees = data.Float("rotation");
            if (!data.Bool("wave", true))
            {
                Remove(sine);
                sine.Counter = 0f;
                RotatedUpdateY();
            }
        }

        internal void RotatedUpdateY() =>
            sprite.Position = flash.Position = bloom.Position = GetRotationVector(sine.Value * 2);

        public Vector2 GetRotationVector(float length) =>
            Vector2Geometry.AngleToVector(-1.5707964f + rotationDegrees * 0.017453292f, length);
    }
}
