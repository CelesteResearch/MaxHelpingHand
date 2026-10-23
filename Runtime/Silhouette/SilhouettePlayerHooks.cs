using System;
using System.Runtime.CompilerServices;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Processes;
using Celeste.Mods.MaxHelpingHand.Sessions;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Presentation.Sprites;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Runtime.Silhouette
{
    public sealed class SilhouettePlayerHooks : IDisposable
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, SilhouettePlayerHooks> owners = new();
        private static readonly PackedColor ZeroDashBlink = new(0x34, 0x8D, 0xC1);
        private readonly ISourceEngineContext engine;
        private readonly Func<MaxHelpingHandSession> session;
        private readonly ILHook renderHook;
        private readonly Hook spriteHook, resetHook;
        public MaxHelpingHandSession Session => session();

        public static SilhouettePlayerHooks For(ISourceEngineContext engine) =>
            owners.TryGetValue(engine, out var owner)
                ? owner
                : throw new InvalidOperationException(
                    "MaxHelpingHand silhouette hooks are not loaded for this engine.");

        public SilhouettePlayerHooks(ISourceEngineContext engine, Func<MaxHelpingHandSession> session)
        {
            this.engine = engine;
            this.session = session;
            owners.Add(engine, this);
            using (new DetourConfigContext(new DetourConfig("MaddieHelpingHand_BeforeAll").WithPriority(int.MinValue))
                       .Use())
                renderHook = new ILHook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.Render)),
                    PatchPlayerRender);
            using (new DetourConfigContext(new DetourConfig("MaddieHelpingHand_AfterAll").WithPriority(int.MaxValue))
                       .Use())
            {
                spriteHook = new Hook(
                    typeof(PlayerSprite).GetConstructor(
                        new[] { typeof(ISourceEngineContext), typeof(PlayerSpriteMode) }),
                    (Action<Action<PlayerSprite, ISourceEngineContext, PlayerSpriteMode>, PlayerSprite,
                        ISourceEngineContext, PlayerSpriteMode>)OnPlayerSpriteConstructor);
                resetHook = new Hook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.ResetSprite)),
                    (Action<Action<RoomPlayerSession, PlayerSpriteMode>, RoomPlayerSession, PlayerSpriteMode>)
                    OnPlayerResetSprite);
            }
        }

        public void Dispose()
        {
            spriteHook.Dispose();
            renderHook.Dispose();
            resetHook.Dispose();
            owners.Remove(engine);
        }

        private static bool IsMadelineMode(PlayerSpriteMode mode) => mode == PlayerSpriteMode.Madeline ||
                                                                     mode == PlayerSpriteMode.MadelineAsBadeline ||
                                                                     mode == PlayerSpriteMode.MadelineNoBackpack;

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void OnPlayerSpriteConstructor(Action<PlayerSprite, ISourceEngineContext, PlayerSpriteMode> original,
            PlayerSprite self, ISourceEngineContext owner, PlayerSpriteMode mode)
        {
            if (ReferenceEquals(owner, engine) && Session.MadelineIsSilhouette && IsMadelineMode(mode))
                mode = PlayerSpriteMode.Playback;
            original(self, owner, mode);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void OnPlayerResetSprite(Action<RoomPlayerSession, PlayerSpriteMode> original, RoomPlayerSession player,
            PlayerSpriteMode mode)
        {
            if (ReferenceEquals(player.EngineContext, engine) && Session.MadelineIsSilhouette && IsMadelineMode(mode))
                mode = PlayerSpriteMode.Playback;
            original(player, mode);
        }

        private void PatchPlayerRender(ILContext il)
        {
            var cursor = new ILCursor(il);
            if (cursor.TryGotoNext(MoveType.After, i => i.MatchCall<PackedColor>("get_Red")))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<PackedColor, RoomPlayerSession, PackedColor>>(OverrideBlinking);
            }

            cursor.Index = 0;
            if (cursor.TryGotoNext(MoveType.After, i => i.MatchCall<PackedColor>("get_White")))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<PackedColor, RoomPlayerSession, PackedColor>>(OverrideBodyColorWithHairColor);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private PackedColor OverrideBlinking(PackedColor color, RoomPlayerSession player)
        {
            if (ReferenceEquals(player.EngineContext, engine) && Session.MadelineIsSilhouette)
            {
                if (player.Data.Dashes == 0) color = ZeroDashBlink;
                player.Hair.Color = color;
            }

            return color;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private PackedColor OverrideBodyColorWithHairColor(PackedColor original, RoomPlayerSession player) =>
            ReferenceEquals(player.EngineContext, engine) && Session.MadelineIsSilhouette
                ? player.Hair.Color
                : original;

        public void RefreshPlayerSpriteMode(RoomPlayerSession player, bool enableSilhouette)
        {
            var target = enableSilhouette
                ? PlayerSpriteMode.Playback
                : ((IOriginalGameplayProcess)engine).SaveContext.CurrentSave.Assists.PlayAsBadeline
                    ? PlayerSpriteMode.MadelineAsBadeline
                    : player.DefaultSpriteMode;
            if (player.Active) player.ResetSpriteNextFrame(target);
            else player.ResetSprite(target);
        }
    }
}
