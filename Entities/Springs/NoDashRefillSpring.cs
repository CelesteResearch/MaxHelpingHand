using System;
using System.Numerics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Interaction.Springs;
using Celeste.Mods.MaxHelpingHand.Interop;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Interaction.Springs;
using Celeste.Simulation.Gameplay.Player.Collision.Components;

namespace Celeste.Mods.MaxHelpingHand.Entities.Springs
{
    [CustomEntity("MaxHelpingHand/NoDashRefillSpring = Create", "MaxHelpingHand/NoDashRefillSpringLeft = Create",
        "MaxHelpingHand/NoDashRefillSpringRight = Create")]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.NoDashRefillSpring")]
    [SceneTracked(true)]
    public class NoDashRefillSpring : OriginalSpringState
    {
        private readonly ISourceEngineContext engine;
        internal readonly bool ignoreLighting;
        private readonly bool refillStamina;
        private float inactiveTimer;

        public static NoDashRefillSpring Create(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(level.EngineContext, data, offset);

        public NoDashRefillSpring(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(data.Position + offset, GetOrientationFromName(data.Name), data.Bool("playerCanUse", true),
                (IOriginalSpringProcess)engine)
        {
            this.engine = engine;
            ignoreLighting = data.Bool("ignoreLighting", false);
            refillStamina = data.Bool("refillStamina", true);
            foreach (var component in OwnedComponents)
                if (component.GetType() == typeof(PlayerColliderComponentState))
                {
                    OwnedComponents.Remove(component);
                    break;
                }

            if (data.Bool("playerCanUse", true))
                OwnedComponents.Add(new PlayerColliderComponentState(OnCollideOverride));
            var sprite = SpringSprite;
            sprite.Reset(SpringProcess.GameGraphics.GameAtlas,
                data.Attr("spriteDirectory", "objects/MaxHelpingHand/noDashRefillSpring") + "/");
            sprite.Add("idle", "", 0f, 0);
            sprite.Add("bounce", "", .07f, "idle", 0, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 4, 5);
            sprite.Add("disabled", "white", .07f);
            sprite.Play("idle");
            sprite.Origin.X = sprite.Width / 2f;
            sprite.Origin.Y = sprite.Height;
        }

        private static SpringOrientation GetOrientationFromName(string name) => name switch
        {
            "MaxHelpingHand/NoDashRefillSpring" or "MaxHelpingHand/CustomDashRefillSpring" => SpringOrientation.Floor,
            "MaxHelpingHand/NoDashRefillSpringLeft" or "MaxHelpingHand/CustomDashRefillSpringLeft" => SpringOrientation
                .WallLeft,
            "MaxHelpingHand/NoDashRefillSpringRight" or "MaxHelpingHand/CustomDashRefillSpringRight" =>
                SpringOrientation.WallRight,
            _ => throw new Exception("Custom Dash Refill Spring name doesn't correlate to a valid Orientation!")
        };

        public override void Update()
        {
            base.Update();
            if (inactiveTimer > 0f) inactiveTimer -= engine.DeltaTime;
        }

        private void OnCollideOverride(RoomPlayerSession player)
        {
            if (player.Machine.State == 9) return;
            int originalDashCount = player.Data.Dashes;
            float originalStamina = player.Data.Stamina;
            bool bounced = false;
            bool inverted = GravityHelperImports.For(engine).IsPlayerInverted();
            float realY = inverted ? -player.Data.Motion.Normal.Speed.Y : player.Data.Motion.Normal.Speed.Y;
            if (Orientation == SpringOrientation.Floor)
            {
                if (realY >= 0f)
                {
                    if (inverted && inactiveTimer <= 0f)
                    {
                        InvertedSuperBounce(player, Top - player.Height);
                        inactiveTimer = .1f;
                        bounced = true;
                    }
                    else if (!inverted)
                    {
                        player.SuperBounce(Top);
                        bounced = true;
                    }
                }
            }
            else if (Orientation == SpringOrientation.WallLeft)
            {
                if (player.SideBounce(1, Right, CenterY)) bounced = true;
            }
            else if (Orientation == SpringOrientation.WallRight)
            {
                if (player.SideBounce(-1, Left, CenterY)) bounced = true;
            }
            else throw new Exception("Orientation not supported!");

            if (bounced)
            {
                AnimateSpringBounce();
                player.Data.Dashes = originalDashCount;
                if (!refillStamina) player.Data.Stamina = originalStamina;
                RefillDashes(player);
            }
        }

        private void InvertedSuperBounce(RoomPlayerSession player, float fromY)
        {
            player.SuperBounce(fromY);
            player.Data.Motion.Normal.Speed.Y *= -1f;
            player.Data.Motion.Normal.VariableJumpSpeed = player.Data.Motion.Normal.Speed.Y;
        }

        protected virtual void RefillDashes(RoomPlayerSession player)
        {
        }
    }
}
