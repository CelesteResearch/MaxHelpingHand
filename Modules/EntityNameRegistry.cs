using System.Collections.Generic;
using Celeste.Core.Reflection.Runtime;
using MonoMod.ModInterop;

namespace Celeste.Mods.MaxHelpingHand.Modules
{
    [ModExportName("MaddieHelpingHand/EntityIdRegistry")]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Module.EntityNameRegistry")]
    public static class EntityNameRegistry
    {
        internal static readonly HashSet<string> SidewaysJumpThrus = new()
        {
            "MaxHelpingHand/SidewaysJumpThru",
            "MaxHelpingHand/AttachedSidewaysJumpThru",
            "MaxHelpingHand/OneWayInvisibleBarrierHorizontal",
            "MaxHelpingHand/SidewaysMovingPlatform"
        };

        public static void RegisterSidewaysJumpThru(string name) => SidewaysJumpThrus.Add(name);
    }
}
