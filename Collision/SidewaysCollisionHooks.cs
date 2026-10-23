using System;
using System.Collections.Generic;
using Celeste.Core.Scheduling;
using Celeste.Game.Content.Maps;
using Celeste.Game.Execution.LevelLoader;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Menus.Overworld;
using Celeste.Mods.MaxHelpingHand.Modules;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Collision
{
    internal sealed class SidewaysCollisionHooks : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly List<IDisposable> loaders = new();
        private readonly List<IDisposable> active = new();
        private bool enabled;

        internal SidewaysCollisionHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            try
            {
                foreach (var constructor in typeof(OriginalLevelLoaderScene).GetConstructors())
                    loaders.Add(new ILHook(constructor, PatchLevelLoader));
                foreach (var constructor in typeof(OverworldLoaderSequence).GetConstructors())
                    loaders.Add(new ILHook(constructor, PatchOverworldLoader));
                if (engine.Frame.Scenes.CurrentScene is OriginalLevelSceneRoot level)
                    Configure(level.Session.MapData as OriginalChapterMapData);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void PatchLevelLoader(ILContext il)
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
        }

        private void PatchOverworldLoader(ILContext il)
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
        }

        private void Configure(OriginalChapterMapData map) => SetEnabled(HasSideways(map));

        private static bool HasSideways(OriginalChapterMapData map)
        {
            if (map?.Levels != null)
                foreach (var level in map.Levels)
                    if (level.Entities != null)
                        foreach (var entity in level.Entities)
                            if (EntityNameRegistry.SidewaysJumpThrus.Contains(entity.Name))
                            {
                                return true;
                            }

            return false;
        }

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
                // VivHelper's native ordering requires its SolidModifierComponent hooks after this ID.
                using (new DetourConfigContext(new DetourConfig("MaxHelpingHand")).Use())
                {
                    active.Add(new SidewaysPlayerHooks(engine));
                    active.Add(new ActorOneWayCollisionHooks(engine, true, false));
                    active.Add(new PlatformCollisionHooks(engine, true, false));
                }
            }
            catch
            {
                SetEnabled(false);
                throw;
            }
        }

        public void Dispose()
        {
            SetEnabled(false);
            for (int i = loaders.Count - 1; i >= 0; i--) loaders[i].Dispose();
            loaders.Clear();
        }
    }
}
