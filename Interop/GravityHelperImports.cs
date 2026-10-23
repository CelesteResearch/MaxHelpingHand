using System;
using System.Runtime.CompilerServices;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using MonoMod.ModInterop;

namespace Celeste.Mods.MaxHelpingHand.Interop
{
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Module.GravityHelperImports")]
    public sealed class GravityHelperImports
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, GravityHelperImports> owners = new();
        internal Interop Imports { get; } = new();

        private GravityHelperImports()
        {
        }

        internal static GravityHelperImports For(ISourceEngineContext engine) =>
            owners.GetValue(engine, _ => new GravityHelperImports());

        public bool IsPlayerInverted() => Imports.IsPlayerInverted?.Invoke() ?? false;

        [ModImportName("GravityHelper")]
        [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Module.GravityHelperImports+Interop")]
        public sealed class Interop
        {
            public Func<bool> IsPlayerInverted;
        }
    }
}
