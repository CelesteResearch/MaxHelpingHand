using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Game.Content.Maps;
using Celeste.Game.Execution.LevelLoader;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Menus.Overworld;
using Celeste.Mods.MaxHelpingHand.Entities;
using Celeste.Physics.Collision;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Player.Collision.Contacts;
using Celeste.Simulation.Gameplay.Player.Collision.Responses;
using Celeste.Simulation.Gameplay.Player.Movement;
using Celeste.Simulation.Gameplay.Player.State;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Collision
{
    internal sealed partial class UpsideDownPlayerHooks : IDisposable
    {
        private static readonly DetourConfig playerHookConfig = new("MaxHelpingHand", priority: int.MaxValue);
        private readonly ISourceEngineContext engine;
        private readonly string ceilingKey;
        private readonly List<IDisposable> loaders = new();
        private readonly List<IDisposable> active = new();
        private bool enabled;

        public UpsideDownPlayerHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            ceilingKey = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog.Find(typeof(UpsideDownJumpThru))
                .RegistryKey;
            try
            {
                foreach (var ctor in typeof(OriginalLevelLoaderScene).GetConstructors())
                    loaders.Add(new ILHook(ctor, il =>
                    {
                        var cursor = new ILCursor(il);
                        while (cursor.TryGotoNext(MoveType.Before, i => i.OpCode == OpCodes.Ret))
                        {
                            cursor.Emit(OpCodes.Ldarg_0);
                            cursor.EmitDelegate<Action<OriginalLevelLoaderScene>>(loader =>
                            {
                                if (ReferenceEquals(loader.EngineContext, engine))
                                    Configure(loader.Session.MapData as OriginalChapterMapData);
                            });
                            cursor.Index++;
                        }
                    }));
                foreach (var ctor in typeof(OverworldLoaderSequence).GetConstructors())
                    loaders.Add(new ILHook(ctor, il =>
                    {
                        var cursor = new ILCursor(il);
                        while (cursor.TryGotoNext(MoveType.Before, i => i.OpCode == OpCodes.Ret))
                        {
                            cursor.Emit(OpCodes.Ldarg_0);
                            cursor.Emit(OpCodes.Ldarg_2);
                            cursor.EmitDelegate<Action<OverworldLoaderSequence, OverworldStartMode>>((loader, mode) =>
                            {
                                if (ReferenceEquals(loader.EngineContext, engine) && (int)mode != -1) SetEnabled(false);
                            });
                            cursor.Index++;
                        }
                    }));
                if (engine.Frame.Scenes.CurrentScene is OriginalLevelSceneRoot level)
                    Configure(level.Session.MapData as OriginalChapterMapData);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void Configure(OriginalChapterMapData map) => SetEnabled(map?.Levels?.Any(level =>
            level.Entities?.Any(entity => entity.Name is "MaxHelpingHand/UpsideDownJumpThru" or
                "MaxHelpingHand/UpsideDownMovingPlatform") == true) == true);

        private void SetEnabled(bool value)
        {
            if (value == enabled) return;
            enabled = value;
            if (!value)
            {
                for (int i = active.Count - 1; i >= 0; i--) active[i].Dispose();
                active.Clear();
                return;
            }

            try
            {
                active.Add(new ActorOneWayCollisionHooks(engine, false, true));
                active.Add(new PlatformCollisionHooks(engine, false, true, playerHookConfig));
                active.Add(new Hook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.Update)),
                    (Action<Action<RoomPlayerSession>, RoomPlayerSession>)Update, playerHookConfig));
                active.Add(new Hook(typeof(PlayerContacts).GetProperty(nameof(PlayerContacts.CanUnDuck)).GetMethod,
                    (Func<Func<PlayerContacts, bool>, PlayerContacts, bool>)CanUnDuck, playerHookConfig));
                active.Add(new Hook(typeof(PlayerCollisionResponse).GetMethod(nameof(PlayerCollisionResponse.Vertical)),
                    (Action<Action<PlayerCollisionResponse, PixelCollision, SourcePlatformEntityState>,
                        PlayerCollisionResponse,
                        PixelCollision, SourcePlatformEntityState>)Vertical, playerHookConfig));
                active.Add(new ILHook(typeof(ClimbSequence).GetMethod(nameof(ClimbSequence.Update)), PatchClimb,
                    playerHookConfig));
                RegisterFloorQueries();
            }
            catch
            {
                SetEnabled(false);
                throw;
            }
        }

        private bool Own(RoomPlayerSession player) => ReferenceEquals(player.EngineContext, engine);

        private bool Ceiling(RoomPlayerSession player, Vector2? at = null, bool outside = false)
        {
            var ceilings = ((ISourceScene)player.Scene).SourceScene.Entities.GetTrackedEntities(ceilingKey);
            for (int i = 0; i < ceilings.Count; i++)
            {
                var ceiling = (UpsideDownJumpThru)ceilings[i];
                if (outside && SourceEntityCollision.Check(player, ceiling)) continue;
                if (at.HasValue
                        ? SourceEntityCollision.CheckAt(player, ceiling, at.Value)
                        : SourceEntityCollision.Check(player, ceiling)) return true;
            }

            return false;
        }

        public void Dispose()
        {
            SetEnabled(false);
            for (int i = loaders.Count - 1; i >= 0; i--) loaders[i].Dispose();
            loaders.Clear();
            movingCeilingType = null;
        }
    }
}
