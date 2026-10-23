using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Startup;
using Celeste.Graphics.Rendering.Drawing;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.MaxHelpingHand.Entities.Collectibles.Strawberries.MultiRoom
{
    [SceneTracked(false)]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.MultiRoomStrawberryCounter")]
    public class MultiRoomStrawberryCounter : SceneEntityState
    {
        private const int normalDepth = -10250;
        private const int depthInFrontOfSolids = -13001;
        private static readonly HashSet<int> hiddenPlayerStates = new() { 13, 23, 14, 25, 15, 12, 18, 11 };
        private readonly List<SourceSolidEntityState> collisionBuffer = new();
        private bool collectingCollisions;
        private float timer = 5f;
        private readonly string text;
        private readonly ISourceEngineContext engine;
        private readonly MultiRoomStrawberryCounterNumbers numbers;

        public static void Initialize(ISourceEngineContext engine)
            => MultiRoomStrawberryCounterNumbers.For(engine).Initialize(engine);

        public MultiRoomStrawberryCounter(string text, ISourceEngineContext engine)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
            Tags |= ((IOriginalGameTagsEnvironment)engine).Tags.Persistent.Value;
            this.text = text;
            this.engine = engine;
            numbers = MultiRoomStrawberryCounterNumbers.For(engine);
        }

        public override void Update()
        {
            base.Update();
            timer -= engine.DeltaTime;
            if (timer <= 0f) RemoveSelf();
            bool inFront = false;
            if (Collider != null)
            {
                bool previousCollecting = collectingCollisions;
                var collisions = previousCollecting ? new List<SourceSolidEntityState>() : collisionBuffer;
                collisions.Clear();
                collectingCollisions = true;
                try
                {
                    foreach (SourceSolidEntityState solid in ((ISourceScene)Scene).SourceScene.Entities
                             .GetTrackedEntities(typeof(SourceSolidEntityState)))
                        if (SourceEntityCollision.Check(this, solid))
                            collisions.Add(solid);
                    foreach (var solid in collisions)
                        if (solid.Depth < normalDepth)
                        {
                            inFront = true;
                            break;
                        }
                }
                finally
                {
                    collectingCollisions = previousCollecting;
                }
            }

            Depth = inFront ? depthInFrontOfSolids : normalDepth;
        }

        public override void Render(ISourceRenderContext context)
        {
            base.Render(context);
            float minX = float.MaxValue, maxX = float.MaxValue, minY = float.MaxValue, maxY = float.MaxValue;
            var player = ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>();
            if (player != null)
            {
                if (hiddenPlayerStates.Contains(player.Machine.State)) return;
                int totalWidth = text.Length * 4 - 1;
                for (int i = 0; i < text.Length; i++)
                {
                    Vector2 position = player.Center + new Vector2(-totalWidth / 2 + i * 4, -18f);
                    int index = text[i] == '/' ? 10 : text[i] - '0';
                    numbers.Numbers[index].DrawOutline((ISourceDrawScope)engine, position, new Vector2(0f, .5f),
                        PackedColor.White);
                    if (minX == float.MaxValue)
                    {
                        minX = maxX = position.X;
                        minY = maxY = position.Y;
                    }
                    else
                    {
                        minX = Math.Min(minX, position.X);
                        maxX = Math.Max(maxX, position.X);
                        minY = Math.Min(minY, position.Y);
                        maxY = Math.Max(maxY, position.Y);
                    }
                }
            }

            Collider = minX != float.MaxValue
                ? new HitboxColliderGeometry(maxX - minX + 5, maxY - minY + 6, minX - 1, minY - 1)
                : null;
        }
    }
}
