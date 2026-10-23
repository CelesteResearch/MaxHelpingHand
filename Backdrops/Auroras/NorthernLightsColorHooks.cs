// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Reflection;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Graphics.Display.Colors;
using Celeste.Game.Presentation.Backdrops.NorthernLights;
using Celeste.Graphics.Rendering.Devices;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Backdrops.Auroras
{
    internal sealed class NorthernLightsColorHooks : IDisposable
    {
        [ThreadStatic] internal static NorthernLightsColorConfiguration Construction;
        [ThreadStatic] internal static PackedColor[] ActiveColors;
        private readonly ISourceEngineContext engine;
        private readonly List<IDisposable> hooks = new List<IDisposable>();
        private static readonly Type backdropType = typeof(NorthernLightsBackdropPresentation);

        private static readonly FieldInfo strandOwner = backdropType.GetNestedType("Strand", BindingFlags.NonPublic)
            .GetField("owner", BindingFlags.Instance | BindingFlags.NonPublic);

        internal NorthernLightsColorHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            try
            {
                hooks.Add(new ILHook(backdropType.GetConstructor(new[] { typeof(ISourceEngineContext) }), Constructor));
                hooks.Add(new ILHook(strandOwner.DeclaringType.GetMethod("Reset"), Reset));
                hooks.Add(new ILHook(backdropType.GetMethod("BeforeRender"), BeforeRender));
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private bool OwnConstruction(NorthernLightsBackdropPresentation self) =>
            self is NorthernLightsCustomColors && Construction != null && ReferenceEquals(Construction.Engine, engine);

        private void Constructor(ILContext il)
        {
            var c = new ILCursor(il);
            while (c.TryGotoNext(MoveType.After, i => i.MatchLdsfld(backdropType, "colors")))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<PackedColor[], NorthernLightsBackdropPresentation, PackedColor[]>>((value, self) =>
                    OwnConstruction(self) ? Construction.Colors : value);
            }

            c.Index = 0;
            while (c.TryGotoNext(MoveType.After, i => i.MatchLdstr("020825")))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<string, NorthernLightsBackdropPresentation, string>>((value, self) =>
                    OwnConstruction(self) ? CanonicalGradient(Construction.GradientColor1) : value);
            }

            c.Index = 0;
            while (c.TryGotoNext(MoveType.After, i => i.MatchLdstr("170c2f")))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<string, NorthernLightsBackdropPresentation, string>>((value, self) =>
                    OwnConstruction(self) ? CanonicalGradient(Construction.GradientColor2) : value);
            }

            ReplaceCount(c, 50, true, (v, s) => OwnConstruction(s) ? Construction.ParticleCount : v);
            ReplaceCount(c, 1024, true, (v, s) => OwnConstruction(s) ? 234 * Construction.StrandCount : v);
            ReplaceCount(c, 3, false, (v, s) => OwnConstruction(s) ? Construction.StrandCount : v);
        }

        private static string CanonicalGradient(string input)
        {
            // The vanilla port parser expects six digits; invoke the original Calc parser
            // at this exact constructor read point before feeding the resulting RGB bytes.
            var color = SourceHexColor.HexToColor(input);
            return color.R.ToString("X2") + color.G.ToString("X2") + color.B.ToString("X2");
        }

        private static void ReplaceCount(ILCursor c, int value, bool array,
            Func<int, NorthernLightsBackdropPresentation, int> replace)
        {
            c.Index = 0;
            // Each of these first literals has a unique original constructor role.
            // Keep the constant instruction so independently owned IL hooks compose.
            c.GotoNext(MoveType.After, i => i.MatchLdcI4(value));
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate(replace);
        }

        private void Reset(ILContext il)
        {
            var c = new ILCursor(il);
            while (c.TryGotoNext(MoveType.After, i => i.MatchLdsfld(backdropType, "colors")))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldfld, strandOwner);
                c.EmitDelegate<Func<PackedColor[], NorthernLightsBackdropPresentation, PackedColor[]>>((value, self) =>
                {
                    if (OwnConstruction(self)) return Construction.Colors;
                    return self is NorthernLightsCustomColors custom && ReferenceEquals(custom.Engine, engine)
                        ? ActiveColors ?? value
                        : value;
                });
            }
        }

        private void BeforeRender(ILContext il)
        {
            var c = new ILCursor(il);
            c.GotoNext(MoveType.After, IsDrawVertices);
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<NorthernLightsBackdropPresentation>>(self =>
            {
                if (self is NorthernLightsCustomColors custom && ReferenceEquals(custom.Engine, engine) &&
                    !custom.DisplayBackground)
                    ((ISourceGraphicsDeviceEnvironment)engine).GraphicsDevice.Clear(PackedColor.Transparent);
            });
            c.GotoNext(MoveType.Before, IsDrawVertices);
            var draw = c.Next;
            var target = c.Next.Next;
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldfld, backdropType.GetField("verts", BindingFlags.Instance | BindingFlags.NonPublic));
            c.Emit(OpCodes.Ldlen);
            c.Emit(OpCodes.Ldc_I4_0);
            c.Emit(OpCodes.Bne_Un, draw);
            for (int i = 0; i < ((MethodReference)draw.Operand).Parameters.Count; i++) c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Br, target);
            c.Index = 0;
            c.GotoNext(MoveType.Before, i => i.OpCode == OpCodes.Call &&
                                             i.Operand is MethodReference method && method.Name == "Blur");
            // Locate the actual bool local consumed by Blur; vertex count is an int.
            // Existing independently owned delegates retain this original load instruction.
            while (c.Index > 0 && (!c.Prev.MatchLdloc(out int local) ||
                                   il.Body.Variables[local].VariableType.MetadataType != MetadataType.Boolean))
                c.Index--;
            if (c.Index == 0)
                throw new InvalidOperationException("Northern lights blur clear bool load is absent.");
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<bool, NorthernLightsBackdropPresentation, bool>>((value, self) =>
                self is NorthernLightsCustomColors custom && ReferenceEquals(custom.Engine, engine) &&
                !custom.DisplayBackground || value);
        }

        private static bool IsDrawVertices(Instruction instruction) => instruction.OpCode == OpCodes.Call &&
                                                                       instruction.Operand is MethodReference method &&
                                                                       method.Name == "DrawVertices";

        public void Dispose()
        {
            for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
            hooks.Clear();
        }
    }
}
