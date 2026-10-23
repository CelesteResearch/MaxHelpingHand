using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Game.Gameplay.Platforms.Crumble;
using Celeste.Graphics.Animation;
using Celeste.Mods.MaxHelpingHand.Entities.Crumble;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Runtime
{
    internal sealed class CrumblePlatformHooks : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private ILHook added;
        private ILHook tileOut;
        private FieldInfo tileImage;

        public CrumblePlatformHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            try
            {
                added = new ILHook(
                    typeof(OriginalCrumblePlatformState).GetMethod(nameof(OriginalCrumblePlatformState.Added)),
                    ModifyAdded);
                var iterator = typeof(OriginalCrumblePlatformState)
                    .GetMethod("TileOut", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetCustomAttribute<IteratorStateMachineAttribute>().StateMachineType;
                tileImage = iterator.GetField("img",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (tileImage == null) throw new MissingFieldException(iterator.FullName, "img");
                tileOut = new ILHook(
                    iterator.GetMethod("MoveNext",
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
                    ModifyTileOut);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void ModifyAdded(ILContext il)
        {
            var cursor = new ILCursor(il);
            int count = 0;
            while (cursor.TryGotoNext(MoveType.After,
                       instruction => instruction.MatchLdstr("objects/crumbleBlock/outline")))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<string, OriginalCrumblePlatformState, string>>((original, entity) =>
                    entity is CustomizableCrumblePlatform custom && custom.MatchesEngine(engine)
                        ? custom.outlineTexture
                        : original);
                count++;
            }

            if (count != 1)
                throw new InvalidOperationException($"Crumble outline hook expected 1 site, found {count}.");
        }

        private void ModifyTileOut(ILContext il)
        {
            var cursor = new ILCursor(il);
            int count = 0;
            while (cursor.TryGotoNext(MoveType.After, instruction => instruction.MatchCall<PackedColor>("get_Gray")))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld, tileImage);
                cursor.EmitDelegate<Func<PackedColor, SpriteImage, PackedColor>>((original, image) =>
                    image.Entity is CustomizableCrumblePlatform custom && custom.MatchesEngine(engine)
                        ? custom.fadeOutTint
                        : original);
                count++;
            }

            if (count != 2) throw new InvalidOperationException($"Crumble fade hook expected 2 sites, found {count}.");
        }

        public void Dispose()
        {
            tileOut?.Dispose();
            tileOut = null;
            added?.Dispose();
            added = null;
        }
    }
}
