using System;
using System.Runtime.CompilerServices;
using System.Linq;
using Celeste.Core.Scheduling;
using Celeste.Everest.Reflection.Runtime;
using Celeste.Everest.Modules.Runtime;
using Celeste.Game.Gameplay.Environment.Lava.Rising;
using Celeste.Mods.MaxHelpingHand.Triggers.Lava;
using Celeste.Physics.Collision;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Runtime.Lava
{
    internal sealed class InstantLavaBlockerHooks : IDisposable
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, InstantLavaBlockerHooks> owners = new();
        private readonly ISourceEngineContext engine;
        private readonly string key;
        private readonly ILHook hook;
        private ILHook frostHook;

        internal static InstantLavaBlockerHooks For(ISourceEngineContext engine) =>
            owners.GetValue(engine, e => new InstantLavaBlockerHooks(e));

        private InstantLavaBlockerHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            key = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog.Find(typeof(InstantLavaBlockerTrigger))
                .RegistryKey;
            hook = new ILHook(typeof(OriginalRisingLavaState).GetMethod(nameof(OriginalRisingLavaState.Update)), Patch);
        }

        public void HookMods()
        {
            if (frostHook != null) return;
            var module = ((ISourceEverestModulesEnvironment)engine).Modules.FirstOrDefault(m =>
                m.Metadata?.Name == "FrostHelper" && m.Metadata.Version >= new Version(1, 34, 4));
            if (module == null) return;
            Type type =
                ((ISourceRuntimeTypeDiscoveryEnvironment)engine).RuntimeTypeDiscovery.ResolveRuntimeTypeAlias(
                    "FrostHelper.CustomRisingLava");
            frostHook = new ILHook(type.GetMethod("Update"), Patch);
        }

        private void Patch(ILContext il)
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.Before,
                    i => i.MatchCallOrCallvirt<RoomPlayerSession>("get_JustRespawned"))) return;
            cursor.Emit(OpCodes.Dup);
            cursor.Index++;
            cursor.EmitDelegate<Func<RoomPlayerSession, bool, bool>>(Block);
        }

        internal bool Block(RoomPlayerSession player, bool original)
        {
            if (!original || !ReferenceEquals(player.EngineContext, engine)) return original;
            var triggers = ((ISourceScene)player.Scene).SourceScene.Entities.GetTrackedEntities(key);
            for (int i = 0; i < triggers.Count; i++)
                if (SourceEntityCollision.Check(player, (InstantLavaBlockerTrigger)triggers[i]))
                    return false;
            return original;
        }

        public void Dispose()
        {
            hook.Dispose();
            frostHook?.Dispose();
            frostHook = null;
            owners.Remove(engine);
        }
    }
}
