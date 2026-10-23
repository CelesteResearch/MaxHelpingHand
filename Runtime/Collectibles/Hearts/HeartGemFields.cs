using System;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using Celeste.Game.Entities.Collectibles.Hearts;
using Celeste.Graphics.Animation;

namespace Celeste.Mods.MaxHelpingHand.Runtime.Collectibles.Hearts
{
    internal static class HeartGemFields
    {
        internal static readonly Func<OriginalHeartGemState, Sprite> GetSprite = Getter<Sprite>("sprite");
        internal static readonly Func<OriginalHeartGemState, float> GetTimer = Getter<float>("timer");

        internal static readonly Func<OriginalHeartGemState, Vector2> GetMoveDirection =
            Getter<Vector2>("moveWiggleDir");

        internal static readonly Func<OriginalHeartGemState, Celeste.Scene.Components.Oscillation.WigglerComponent>
            GetMoveWiggler = Getter<Celeste.Scene.Components.Oscillation.WigglerComponent>("moveWiggler");

        internal static readonly
            Action<OriginalHeartGemState,
                Celeste.Simulation.Presentation.Particles.Definitions.ParticleTypePresentation> SetShine =
                Setter<Celeste.Simulation.Presentation.Particles.Definitions.ParticleTypePresentation>("shineParticle");

        private static Func<OriginalHeartGemState, T> Getter<T>(string name)
        {
            var field =
                typeof(OriginalHeartGemState).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic) ??
                throw new MissingFieldException(name);
            var method = new DynamicMethod("GetHeart" + name, typeof(T), new[] { typeof(OriginalHeartGemState) },
                typeof(HeartGemFields), true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Ret);
            return (Func<OriginalHeartGemState, T>)method.CreateDelegate(
                typeof(Func<OriginalHeartGemState, T>));
        }

        private static Action<OriginalHeartGemState, T> Setter<T>(string name)
        {
            var field =
                typeof(OriginalHeartGemState).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic) ??
                throw new MissingFieldException(name);
            var method = new DynamicMethod("SetHeart" + name, typeof(void),
                new[] { typeof(OriginalHeartGemState), typeof(T) }, typeof(HeartGemFields), true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, field);
            il.Emit(OpCodes.Ret);
            return (Action<OriginalHeartGemState, T>)method.CreateDelegate(
                typeof(Action<OriginalHeartGemState, T>));
        }
    }
}
