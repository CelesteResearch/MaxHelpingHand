using System;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using Celeste.Game.Gameplay.Collectibles.Strawberries;
using Celeste.Graphics.Animation;
using Celeste.Simulation;
using Celeste.Scene.Following;

namespace Celeste.Mods.MaxHelpingHand.Runtime.Collectibles.Strawberries.MultiRoom
{
    internal static class StrawberrySeedFields
    {
        internal static readonly Func<OriginalStrawberrySeedState, IOriginalStrawberryProcess> GetProcess =
            Getter<IOriginalStrawberryProcess>("process");

        internal static readonly Func<OriginalStrawberrySeedState, FollowerState> GetFollower =
            Getter<FollowerState>("follower");

        internal static readonly Func<OriginalStrawberrySeedState, Sprite> GetSprite = Getter<Sprite>("sprite");
        internal static readonly Action<OriginalStrawberrySeedState, Sprite> SetSprite = Setter<Sprite>("sprite");
        internal static readonly Func<OriginalStrawberrySeedState, int> GetIndex = Getter<int>("index");
        internal static readonly Func<OriginalStrawberrySeedState, bool> GetGhost = Getter<bool>("ghost");
        internal static readonly Func<OriginalStrawberrySeedState, Vector2> GetStart = Getter<Vector2>("start");
        internal static readonly Action<OriginalStrawberrySeedState, Vector2> SetStart = Setter<Vector2>("start");

        internal static readonly Func<OriginalStrawberrySeedState, RoomPlayerSession> GetPlayer =
            Getter<RoomPlayerSession>("player");

        internal static readonly Action<OriginalStrawberrySeedState, RoomPlayerSession> SetPlayer =
            Setter<RoomPlayerSession>("player");

        internal static readonly Action<OriginalStrawberrySeedState, float> SetCanLoseTimer =
            Setter<float>("canLoseTimer");

        private static Func<OriginalStrawberrySeedState, T> Getter<T>(string name)
        {
            var field =
                typeof(OriginalStrawberrySeedState).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic) ??
                throw new MissingFieldException(name);
            var method = new DynamicMethod("GetSeed" + name, typeof(T), new[] { typeof(OriginalStrawberrySeedState) },
                typeof(StrawberrySeedFields), true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Ret);
            return (Func<OriginalStrawberrySeedState, T>)method.CreateDelegate(
                typeof(Func<OriginalStrawberrySeedState, T>));
        }

        private static Action<OriginalStrawberrySeedState, T> Setter<T>(string name)
        {
            var field =
                typeof(OriginalStrawberrySeedState).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic) ??
                throw new MissingFieldException(name);
            var method = new DynamicMethod("SetSeed" + name, typeof(void),
                new[] { typeof(OriginalStrawberrySeedState), typeof(T) }, typeof(StrawberrySeedFields), true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, field);
            il.Emit(OpCodes.Ret);
            return (Action<OriginalStrawberrySeedState, T>)method.CreateDelegate(
                typeof(Action<OriginalStrawberrySeedState, T>));
        }
    }
}
