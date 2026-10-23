using System;
using System.Linq.Expressions;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using Celeste.Core.Display;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Geometry.TwoD.Camera;
using Celeste.Simulation.Presentation.Backdrops.Runtime;
using Celeste.Simulation.Presentation.Backdrops.Weather.Snow;

namespace Celeste.Mods.MaxHelpingHand.Backdrops.Weather.Snow
{
    internal static class SnowFieldAccess
    {
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
        private static readonly Type Snow = typeof(SnowBackdropPresentation);
        private static readonly FieldInfo Particles = Snow.GetField("particles", Fields);
        private static readonly Type Particle = Particles.FieldType.GetElementType();
        internal static readonly Func<SnowBackdropPresentation, float> VisibleFade = Read<float>("visibleFade");
        internal static readonly Action<SnowBackdropPresentation, float> SetVisibleFade = Write<float>("visibleFade");
        internal static readonly Func<SnowBackdropPresentation, float> LinearFade = Read<float>("linearFade");
        internal static readonly Action<SnowBackdropPresentation, float> SetLinearFade = Write<float>("linearFade");
        internal static readonly Func<SnowBackdropPresentation, PackedColor[]> Colors = Read<PackedColor[]>("colors");

        internal static readonly Func<SnowBackdropPresentation, PackedColor[]> BlendedColors =
            Read<PackedColor[]>("blendedColors");

        internal static readonly Action<SnowBackdropPresentation, ISourceBackdropScene> UpdateBackdrop =
            CreateBackdropUpdate();

        internal static readonly Action<SnowBackdropPresentation, PackedColor[], int, float, float> Configure =
            CreateConfigure();

        internal static readonly Action<SnowBackdropPresentation, Vector2, Vector2, float, ISourceEngineContext>
            UpdateParticles = CreateUpdate();

        internal static readonly
            Action<SnowBackdropPresentation, ISourceRenderContext, CameraState, AtlasEntry, Vector2, bool>
            RenderParticles = CreateRender();

        private static Func<SnowBackdropPresentation, T> Read<T>(string name)
        {
            var field = Snow.GetField(name, Fields) ?? throw new MissingFieldException(Snow.FullName, name);
            if (field.FieldType != typeof(T))
                throw new InvalidOperationException("Unexpected Snow field type: " + name);
            var method = new DynamicMethod("ReadSnow" + name, typeof(T), new[] { Snow }, Snow, true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Ret);
            return (Func<SnowBackdropPresentation, T>)method.CreateDelegate(typeof(Func<SnowBackdropPresentation, T>));
        }

        private static Action<SnowBackdropPresentation, T> Write<T>(string name)
        {
            var field = Snow.GetField(name, Fields) ?? throw new MissingFieldException(Snow.FullName, name);
            if (field.FieldType != typeof(T))
                throw new InvalidOperationException("Unexpected Snow field type: " + name);
            var method = new DynamicMethod("WriteSnow" + name, typeof(void), new[] { Snow, typeof(T) }, Snow, true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, field);
            il.Emit(OpCodes.Ret);
            return (Action<SnowBackdropPresentation, T>)method.CreateDelegate(
                typeof(Action<SnowBackdropPresentation, T>));
        }

        private static Action<SnowBackdropPresentation, ISourceBackdropScene> CreateBackdropUpdate()
        {
            var target = typeof(BackdropPresentation).GetMethod(nameof(BackdropPresentation.Update), Fields,
                null, new[] { typeof(ISourceBackdropScene) }, null);
            var method = new DynamicMethod("SnowBackdropBaseUpdate", typeof(void),
                new[] { Snow, typeof(ISourceBackdropScene) }, Snow, true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, target);
            il.Emit(OpCodes.Ret);
            return (Action<SnowBackdropPresentation, ISourceBackdropScene>)method.CreateDelegate(
                typeof(Action<SnowBackdropPresentation, ISourceBackdropScene>));
        }

        private static Expression EachParticle(ParameterExpression snow, ParameterExpression index, Expression body)
        {
            var end = Expression.Label();
            return Expression.Block(new[] { index }, Expression.Assign(index, Expression.Constant(0)),
                Expression.Loop(Expression.IfThenElse(
                    Expression.LessThan(index, Expression.ArrayLength(Expression.Field(snow, Particles))),
                    Expression.Block(body, Expression.PostIncrementAssign(index), Expression.Empty()),
                    Expression.Break(end)), end));
        }

        private static Expression ParticleField(ParameterExpression snow, ParameterExpression index, string name)
            => Expression.Field(Expression.ArrayAccess(Expression.Field(snow, Particles), index),
                Particle.GetField(name, Fields));

        private static Action<SnowBackdropPresentation, PackedColor[], int, float, float> CreateConfigure()
        {
            var snow = Expression.Parameter(Snow, "snow");
            var colors = Expression.Parameter(typeof(PackedColor[]), "colors");
            var count = Expression.Parameter(typeof(int), "count");
            var min = Expression.Parameter(typeof(float), "min");
            var max = Expression.Parameter(typeof(float), "max");
            var index = Expression.Variable(typeof(int), "index");
            var body = Expression.Block(
                Expression.Assign(Expression.Field(snow, "colors"), colors),
                Expression.Assign(Expression.Field(snow, "blendedColors"),
                    Expression.NewArrayBounds(typeof(PackedColor), Expression.ArrayLength(colors))),
                Expression.Assign(Expression.Field(snow, Particles), Expression.NewArrayBounds(Particle, count)),
                EachParticle(snow, index, Expression.Call(
                    Expression.ArrayAccess(Expression.Field(snow, Particles), index),
                    Particle.GetMethod("Init", Fields), snow, Expression.ArrayLength(colors), min, max)));
            return Expression
                .Lambda<Action<SnowBackdropPresentation, PackedColor[], int, float, float>>(body, snow, colors, count,
                    min, max).Compile();
        }

        private static Action<SnowBackdropPresentation, Vector2, Vector2, float, ISourceEngineContext> CreateUpdate()
        {
            var method = new DynamicMethod("UpdateCustomSnowParticles", typeof(void),
                new[] { Snow, typeof(Vector2), typeof(Vector2), typeof(float), typeof(ISourceEngineContext) }, Snow,
                true);
            var il = method.GetILGenerator();
            var index = il.DeclareLocal(typeof(int));
            var loop = il.DefineLabel();
            var check = il.DefineLabel();
            var position = Particle.GetField("Position", Fields);
            var speed = Particle.GetField("Speed", Fields);
            var sin = Particle.GetField("Sin", Fields);
            var multiply = typeof(Vector2).GetMethod("op_Multiply", new[] { typeof(Vector2), typeof(float) });
            var add = typeof(Vector2).GetMethod("op_Addition", new[] { typeof(Vector2), typeof(Vector2) });
            var frame = typeof(ISourceEngineContext).GetProperty("Frame").GetMethod;
            var delta = frame.ReturnType.GetField("DeltaTime");
            Action address = () =>
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, Particles);
                il.Emit(OpCodes.Ldloc, index);
                il.Emit(OpCodes.Ldelema, Particle);
            };
            Action readDelta = () =>
            {
                il.Emit(OpCodes.Ldarg_S, (byte)4);
                il.Emit(OpCodes.Callvirt, frame);
                il.Emit(OpCodes.Ldfld, delta);
            };
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, index);
            il.Emit(OpCodes.Br, check);
            il.MarkLabel(loop);
            address();
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldfld, position);
            il.Emit(OpCodes.Ldarg_1);
            address();
            il.Emit(OpCodes.Ldfld, speed);
            il.Emit(OpCodes.Call, multiply);
            readDelta();
            il.Emit(OpCodes.Call, multiply);
            il.Emit(OpCodes.Call, add);
            il.Emit(OpCodes.Stfld, position);
            address();
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldfld, position);
            il.Emit(OpCodes.Ldarg_2);
            address();
            il.Emit(OpCodes.Ldfld, sin);
            il.Emit(OpCodes.Call, typeof(MathF).GetMethod(nameof(MathF.Sin)));
            il.Emit(OpCodes.Call, multiply);
            address();
            il.Emit(OpCodes.Ldfld, speed);
            il.Emit(OpCodes.Call, multiply);
            il.Emit(OpCodes.Ldarg_3);
            il.Emit(OpCodes.Call, multiply);
            readDelta();
            il.Emit(OpCodes.Call, multiply);
            il.Emit(OpCodes.Call, add);
            il.Emit(OpCodes.Stfld, position);
            address();
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldfld, sin);
            readDelta();
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stfld, sin);
            il.Emit(OpCodes.Ldloc, index);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, index);
            il.MarkLabel(check);
            il.Emit(OpCodes.Ldloc, index);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, Particles);
            il.Emit(OpCodes.Ldlen);
            il.Emit(OpCodes.Conv_I4);
            il.Emit(OpCodes.Blt, loop);
            il.Emit(OpCodes.Ret);
            return (Action<SnowBackdropPresentation, Vector2, Vector2, float, ISourceEngineContext>)method
                .CreateDelegate(
                    typeof(Action<SnowBackdropPresentation, Vector2, Vector2, float, ISourceEngineContext>));
        }

        private static Action<SnowBackdropPresentation, ISourceRenderContext, CameraState, AtlasEntry, Vector2, bool>
            CreateRender()
        {
            var snow = Expression.Parameter(Snow, "snow");
            var context = Expression.Parameter(typeof(ISourceRenderContext), "context");
            var camera = Expression.Parameter(typeof(CameraState), "camera");
            var texture = Expression.Parameter(typeof(AtlasEntry), "texture");
            var padding = Expression.Parameter(typeof(Vector2), "padding");
            var tiled = Expression.Parameter(typeof(bool), "tiled");
            var index = Expression.Variable(typeof(int), "index");
            var color = Expression.ArrayIndex(Expression.Field(snow, "blendedColors"),
                ParticleField(snow, index, "Color"));
            var body = EachParticle(snow, index, Expression.Call(
                typeof(SnowParticleDrawing).GetMethod("RenderParticle", BindingFlags.Static | BindingFlags.NonPublic),
                snow, context, camera, texture, padding, tiled, ParticleField(snow, index, "Position"), color));
            return Expression
                .Lambda<Action<SnowBackdropPresentation, ISourceRenderContext, CameraState, AtlasEntry, Vector2, bool>>(
                    body, snow, context, camera, texture, padding, tiled).Compile();
        }
    }
}
