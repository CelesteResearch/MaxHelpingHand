using System;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using Celeste.Game.Gameplay.Hazards.Bumpers;
using Celeste.Graphics.Animation;

namespace Celeste.Mods.MaxHelpingHand.Runtime.Hazards.Bumpers
{
    internal static class BumperFields
    {
        internal static readonly Func<OriginalBumperState, Sprite> GetSprite = Getter<Sprite>("sprite");
        internal static readonly Func<OriginalBumperState, Sprite> GetEvilSprite = Getter<Sprite>("spriteEvil");

        internal static readonly Func<OriginalBumperState, Celeste.Scene.Components.Oscillation.SineWaveState> GetSine =
            Getter<Celeste.Scene.Components.Oscillation.SineWaveState>("sine");

        internal static readonly Action<OriginalBumperState, bool> SetFireMode = Setter<bool>("fireMode");

        private static Func<OriginalBumperState, T> Getter<T>(string name)
        {
            var field =
                typeof(OriginalBumperState).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic) ??
                throw new MissingFieldException(name);
            var method = new DynamicMethod("GetBumper" + name, typeof(T), new[] { typeof(OriginalBumperState) },
                typeof(BumperFields), true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Ret);
            return (Func<OriginalBumperState, T>)method.CreateDelegate(
                typeof(Func<OriginalBumperState, T>));
        }

        private static Action<OriginalBumperState, T> Setter<T>(string name)
        {
            var field =
                typeof(OriginalBumperState).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic) ??
                throw new MissingFieldException(name);
            var method = new DynamicMethod("SetBumper" + name, typeof(void),
                new[] { typeof(OriginalBumperState), typeof(T) }, typeof(BumperFields), true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, field);
            il.Emit(OpCodes.Ret);
            return (Action<OriginalBumperState, T>)method.CreateDelegate(
                typeof(Action<OriginalBumperState, T>));
        }
    }
}
