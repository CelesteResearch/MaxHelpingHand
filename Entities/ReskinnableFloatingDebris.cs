// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Environment.Debris;
using Celeste.Mods.MaxHelpingHand.Components;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Components.Oscillation;
using Celeste.Simulation;

namespace Celeste.Mods.MaxHelpingHand.Entities
{
    [CustomEntity("MaxHelpingHand/ReskinnableFloatingDebris = Load")]
    public static class ReskinnableFloatingDebris
    {
        public static OriginalFloatingDebrisState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
        {
            var engine = (IOriginalFloatingDebrisProcess)level.EngineContext;
            int width = data.Int("debrisWidth", 8), height = data.Int("debrisHeight", 8);
            var debris = new OriginalFloatingDebrisState(data.Position + offset, engine,
                engine.GameGraphics.GameAtlas[data.Attr("texture", "scenery/debris")], width, height)
            {
                Depth = data.Int("depth", -5),
                Collider = new HitboxColliderGeometry(width + 4, height + 4, -(width / 2) - 2, -(height / 2) - 2),
                Collidable = data.Bool("interactWithPlayer", true)
            };
            string rotation = data.Attr("rotateSpeed");
            if (!string.IsNullOrEmpty(rotation)) debris.RotationSpeed = float.Parse(rotation);
            float scrollX = data.Float("scrollX"), scrollY = data.Float("scrollY");
            if (scrollX != 0 || scrollY != 0)
            {
                var parallax = new FloatingDebrisParallax(debris.Position, new Vector2(scrollX, scrollY));
                debris.Add(parallax);
                debris.Add(new RoomTransitionListener
                    { OnIn = _ => parallax.Update(), OnOut = _ => parallax.Update() });
            }

            if (!data.Bool("floating", true))
            {
                var sine = debris.Get<SineWaveState>();
                debris.Remove(sine);
                sine.Reset();
            }

            return debris;
        }
    }
}
