using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Numerics;
using Celeste.Mods.MaxHelpingHand.Entities.Platforms;
using Celeste.Simulation.Gameplay.Platforms.StaticMovers;

namespace Celeste.Mods.MaxHelpingHand.Runtime.Hazards.Bumpers
{
    internal static class MovingPlatformAttachment
    {
        internal static readonly Action<MultiNodeMovingPlatform, SourceStaticMoverState, Vector2?> Animate = Create();

        private static Action<MultiNodeMovingPlatform, SourceStaticMoverState, Vector2?> Create()
        {
            var target =
                typeof(MultiNodeMovingPlatform).GetMethod("AnimateObject",
                    BindingFlags.Instance | BindingFlags.NonPublic) ??
                throw new MissingMethodException("AnimateObject");
            var method = new DynamicMethod("AttachBumper", typeof(void),
                new[] { typeof(MultiNodeMovingPlatform), typeof(SourceStaticMoverState), typeof(Vector2?) },
                typeof(MovingPlatformAttachment), true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Call, target);
            il.Emit(OpCodes.Ret);
            return (Action<MultiNodeMovingPlatform, SourceStaticMoverState, Vector2?>)method.CreateDelegate(
                typeof(Action<MultiNodeMovingPlatform, SourceStaticMoverState, Vector2?>));
        }
    }
}
