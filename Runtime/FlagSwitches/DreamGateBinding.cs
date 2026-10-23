// SPDX-License-Identifier: MIT

using System;
using System.Linq;
using System.Reflection;
using Celeste.Core.Reflection.Runtime;

namespace Celeste.Mods.MaxHelpingHand.Runtime.FlagSwitches
{
    internal sealed class DreamGateBinding
    {
        private readonly FieldInfo isFlagSwitchGate;
        private readonly MethodInfo flagGetter;
        private readonly MethodInfo triggeredSetter;

        private DreamGateBinding(FieldInfo isFlagSwitchGate, MethodInfo flagGetter, MethodInfo triggeredSetter)
        {
            this.isFlagSwitchGate = isFlagSwitchGate;
            this.flagGetter = flagGetter;
            this.triggeredSetter = triggeredSetter;
        }

        internal static DreamGateBinding Create(Type type)
        {
            const string sourceName = "Celeste.Mod.CommunalHelper.Entities.DreamSwitchGate";
            if (type.FullName != sourceName && !type.GetCustomAttributes<RuntimeTypeAliasAttribute>(false)
                    .Any(alias => alias.Name == sourceName)) return null;
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var field = type.GetField("isFlagSwitchGate", flags) ??
                        throw new MissingFieldException(type.FullName, "isFlagSwitchGate");
            var getter = type.GetProperty("Flag", flags)?.GetGetMethod(true) ??
                         throw new MissingMemberException(type.FullName, "Flag");
            var setter = type.GetProperty("Triggered", flags)?.GetSetMethod(true) ??
                         throw new MissingMemberException(type.FullName, "Triggered");
            return new DreamGateBinding(field, getter, setter);
        }

        internal void Trigger(object entity, string flag)
        {
            if ((bool)isFlagSwitchGate.GetValue(entity) && (string)flagGetter.Invoke(entity, null) == flag)
                triggeredSetter.Invoke(entity, new object[] { true });
        }
    }
}
