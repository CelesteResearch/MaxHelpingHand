// SPDX-License-Identifier: MIT

using System.Numerics;
using Celeste.Core.Components;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Scene.Entities;

namespace Celeste.Mods.MaxHelpingHand.Components
{
    public sealed class FloatingDebrisParallax : SourceComponentState
    {
        private readonly Vector2 position;
        private readonly Vector2 scroll;
        private Vector2? levelPosition;

        public FloatingDebrisParallax(Vector2 position, Vector2 scroll) : base(true, false)
        {
            this.position = position;
            this.scroll = scroll;
        }

        public override void Update()
        {
            base.Update();
            var entity = (SceneEntityState)Entity;
            levelPosition ??= ((OriginalLevelRoomData)((OriginalLevelSceneRoot)entity.Scene).Session.LevelData)
                .Position;
            var camera = (entity.Scene as OriginalLevelSceneRoot)?.Camera;
            if (camera != null) entity.Position = position + (camera.Position - levelPosition.Value) * scroll;
        }
    }
}
