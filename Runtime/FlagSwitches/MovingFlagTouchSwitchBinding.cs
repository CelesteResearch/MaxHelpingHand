// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using System.Reflection;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Everest.Reflection.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Gameplay.Mechanisms.Switches;
using Celeste.Graphics.Animation;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.MaxHelpingHand.Runtime.FlagSwitches
{
    internal sealed class MovingFlagTouchSwitchBinding
    {
        private readonly ConstructorInfo constructor;
        private readonly MethodInfo configure;
        private readonly PropertyInfo movingColor, movingDelay;

        internal MovingFlagTouchSwitchBinding(ISourceEngineContext engine)
        {
            var type =
                ((ISourceRuntimeTypeDiscoveryEnvironment)engine).RuntimeTypeDiscovery.ResolveRuntimeTypeAlias(
                    "Celeste.Mod.OutbackHelper.MovingTouchSwitch") ??
                throw new TypeLoadException("OutbackHelper MovingTouchSwitch capability is missing.");
            constructor =
                type.GetConstructor(new[] { typeof(ISourceEngineContext), typeof(EntityData), typeof(Vector2) }) ??
                throw new MissingMethodException(type.FullName, ".ctor");
            configure = type.GetMethod("ConfigureAppearance",
                new[]
                {
                    typeof(PackedColor), typeof(PackedColor), typeof(PackedColor), typeof(Sprite),
                    typeof(ParticleTypePresentation)
                }) ?? throw new MissingMethodException(type.FullName, "ConfigureAppearance");
            movingColor = type.GetProperty("MovingColor") ??
                          throw new MissingMemberException(type.FullName, "MovingColor");
            movingDelay = type.GetProperty("MovingDelay") ??
                          throw new MissingMemberException(type.FullName, "MovingDelay");
        }

        internal OriginalTouchSwitchState Create(ISourceEngineContext engine, EntityData data, Vector2 offset) =>
            (OriginalTouchSwitchState)constructor.Invoke(new object[] { engine, data, offset });

        internal void SetMotion(OriginalTouchSwitchState moving, PackedColor color, float delay)
        {
            movingColor.SetValue(moving, color);
            movingDelay.SetValue(moving, delay);
        }

        internal void ConfigureAppearance(OriginalTouchSwitchState moving, PackedColor inactive, PackedColor active,
            PackedColor finish, Sprite icon, ParticleTypePresentation fire) =>
            configure.Invoke(moving, new object[] { inactive, active, finish, icon, fire });
    }
}
