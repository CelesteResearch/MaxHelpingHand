using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using Celeste.Core.Display;
using Celeste.Game.Gameplay.Checkpoints.Summit;
using Celeste.Graphics.Content.Atlas;

namespace Celeste.Mods.MaxHelpingHand.Entities.Checkpoints
{
    public partial class CustomSummitCheckpoint
    {
        private static readonly Action<OriginalSummitCheckpointState, string> SetNumberString =
            CreateFieldSetter<string>("numberString");

        private static readonly Action<OriginalSummitCheckpointState, AtlasEntry> SetBaseEmpty =
            CreateFieldSetter<AtlasEntry>("baseEmpty");

        private static readonly Action<OriginalSummitCheckpointState, AtlasEntry> SetBaseToggle =
            CreateFieldSetter<AtlasEntry>("baseToggle");

        private static readonly Action<OriginalSummitCheckpointState, AtlasEntry> SetBaseActive =
            CreateFieldSetter<AtlasEntry>("baseActive");

        private static readonly Action<OriginalSummitCheckpointState, List<AtlasEntry>> SetNumbersEmpty =
            CreateFieldSetter<List<AtlasEntry>>("numbersEmpty");

        private static readonly Action<OriginalSummitCheckpointState, List<AtlasEntry>> SetNumbersActive =
            CreateFieldSetter<List<AtlasEntry>>("numbersActive");

        private static readonly Func<PackedColor[]> GetConfettiColors = CreatePaletteGetter();
        private static readonly Action<PackedColor[]> SetConfettiColors = CreatePaletteSetter();

        private static Action<OriginalSummitCheckpointState, T> CreateFieldSetter<T>(string name)
        {
            var field = typeof(OriginalSummitCheckpointState).GetField(name,
                BindingFlags.NonPublic | BindingFlags.Instance);
            var method = new DynamicMethod("MhhSummit" + name, typeof(void),
                new[] { typeof(OriginalSummitCheckpointState), typeof(T) }, typeof(CustomSummitCheckpoint), true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, field);
            il.Emit(OpCodes.Ret);
            return (Action<OriginalSummitCheckpointState, T>)method.CreateDelegate(
                typeof(Action<OriginalSummitCheckpointState, T>));
        }

        private static FieldInfo PaletteField() =>
            typeof(ConfettiRenderer).GetField("confettiColors", BindingFlags.NonPublic | BindingFlags.Static);

        private static Func<PackedColor[]> CreatePaletteGetter()
        {
            var method = new DynamicMethod("MhhSummitConfettiRead", typeof(PackedColor[]), Type.EmptyTypes,
                typeof(CustomSummitCheckpoint), true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldsfld, PaletteField());
            il.Emit(OpCodes.Ret);
            return (Func<PackedColor[]>)method.CreateDelegate(typeof(Func<PackedColor[]>));
        }

        private static Action<PackedColor[]> CreatePaletteSetter()
        {
            var method = new DynamicMethod("MhhSummitConfettiWrite", typeof(void), new[] { typeof(PackedColor[]) },
                typeof(CustomSummitCheckpoint), true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Stsfld, PaletteField());
            il.Emit(OpCodes.Ret);
            return (Action<PackedColor[]>)method.CreateDelegate(typeof(Action<PackedColor[]>));
        }
    }
}
