using Celeste.Graphics.Geometry.TwoD.Camera;
using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.LinearAlgebra;
using Celeste.Core.Randomness;
using Celeste.Core.Rendering;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Display;
using Celeste.Graphics.Effects.Liquids;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Rendering.Primitives;
using Celeste.Graphics.Rendering.Vertices;

namespace Celeste.Mods.MaxHelpingHand.Components.Lava
{
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.SidewaysLavaRect")]
    public class SidewaysLavaRect : SourceComponentState
    {
        [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias(
            "Celeste.Mod.MaxHelpingHand.Entities.SidewaysLavaRect+OnlyModes")]
        public enum OnlyModes
        {
            OnlyLeft,
            OnlyRight
        }

        [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias(
            "Celeste.Mod.MaxHelpingHand.Entities.SidewaysLavaRect+Bubble")]
        private struct Bubble
        {
            public Vector2 Position;

            public float Speed;

            public float Alpha;

            public bool Expired;
        }

        [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias(
            "Celeste.Mod.MaxHelpingHand.Entities.SidewaysLavaRect+SurfaceBubble")]
        private struct SurfaceBubble
        {
            public float Y;

            public float Frame;

            public byte Animation;
        }

        public Vector2 Position;

        public float Fade = 16f;

        public float Spikey;

        public OnlyModes OnlyMode;

        public float SmallWaveAmplitude = 1f;

        public float BigWaveAmplitude = 4f;

        public float CurveAmplitude = 7f;

        public float UpdateMultiplier = 1f;

        public PackedColor SurfaceColor = PackedColor.White;

        public PackedColor EdgeColor = PackedColor.LightGray;

        public PackedColor CenterColor = PackedColor.DarkGray;

        private float timer;
        private readonly ISourceLavaRectangleEnvironment engine;

        private SourceVertexPositionColor[] verts;

        private int vertCount;

        private bool dirty;

        private Bubble[] bubbles;

        private SurfaceBubble[] surfaceBubbles;

        private int surfaceBubbleIndex;

        private List<List<AtlasEntry>> surfaceBubbleAnimations;

        public int SurfaceStep { get; private set; }

        public float Width { get; private set; }

        public float Height { get; private set; }

        public SidewaysLavaRect(float width, float height, int step, OnlyModes onlyMode,
            ISourceLavaRectangleEnvironment engine)
            : base(true, true)
        {
            this.engine = engine;
            timer = SourceRandomMath.NextFloat(engine.Random, 100f);
            OnlyMode = onlyMode;
            Resize(width, height, step);
            surfaceBubbleAnimations = new List<List<AtlasEntry>>();
            surfaceBubbleAnimations.Add(engine.LiquidAtlas.GetAtlasSubtextures((OnlyMode == OnlyModes.OnlyLeft)
                ? "danger/MaxHelpingHand/sidewayslava/bubble_right_a"
                : "danger/MaxHelpingHand/sidewayslava/bubble_left_a"));
        }

        private void shuffleBubble(int i)
        {
            bubbles[i].Position = new Vector2(1f + SourceRandomMath.NextFloat(engine.Random, Width - 2f),
                SourceRandomMath.NextFloat(engine.Random, Height));
            bubbles[i].Speed = SourceRandomMath.Range(engine.Random, 4, 12);
            bubbles[i].Alpha = SourceRandomMath.Range(engine.Random, 0.4f, 0.8f);
        }

        public void Resize(float width, float height, int step)
        {
            float width2 = Width;
            Width = width;
            Height = height;
            SurfaceStep = step;
            dirty = true;
            int num = (int)(width / (float)SurfaceStep * 2f + height / (float)SurfaceStep * 2f + 4f);
            verts = new SourceVertexPositionColor[num * 3 * 6 + 6];
            Bubble[] array = bubbles ?? new Bubble[0];
            bubbles = new Bubble[(int)(width * height * 0.005f)];
            for (int i = 0; i < array.Length && i < bubbles.Length; i++)
            {
                bubbles[i] = array[i];
                bubbles[i].Expired = true;
                if (OnlyMode == OnlyModes.OnlyLeft)
                {
                    bubbles[i].Position.X += Width - width2;
                }
            }

            for (int j = array.Length; j < bubbles.Length; j++)
            {
                shuffleBubble(j);
            }

            SurfaceBubble[] array2 = surfaceBubbles ?? new SurfaceBubble[0];
            surfaceBubbles = new SurfaceBubble[(int)Math.Max(4f, (float)bubbles.Length * 0.25f)];
            for (int k = 0; k < array2.Length && k < surfaceBubbles.Length; k++)
            {
                surfaceBubbles[k] = array2[k];
            }

            for (int l = array2.Length; l < surfaceBubbles.Length; l++)
            {
                surfaceBubbles[l].Y = -1f;
            }

            surfaceBubbleIndex %= surfaceBubbles.Length;
        }

        public override void Update()
        {
            timer += UpdateMultiplier * engine.DeltaTime;
            if (UpdateMultiplier != 0f)
            {
                dirty = true;
            }

            for (int i = 0; i < bubbles.Length; i++)
            {
                bool flag = false;
                if (OnlyMode == OnlyModes.OnlyLeft)
                {
                    bubbles[i].Position.X += UpdateMultiplier * bubbles[i].Speed * engine.DeltaTime;
                    if (bubbles[i].Position.X >
                        Width - 2f + Wave((int)(bubbles[i].Position.Y / (float)SurfaceStep), Height))
                    {
                        bubbles[i].Position.X = 1f;
                        flag = true;
                    }
                }
                else
                {
                    bubbles[i].Position.X -= UpdateMultiplier * bubbles[i].Speed * engine.DeltaTime;
                    if (bubbles[i].Position.X < 2f - Wave((int)(bubbles[i].Position.Y / (float)SurfaceStep), Height))
                    {
                        bubbles[i].Position.X = Width - 1f;
                        flag = true;
                    }
                }

                if (flag)
                {
                    if (bubbles[i].Expired)
                    {
                        shuffleBubble(i);
                        bubbles[i].Expired = false;
                    }

                    if (SourceRandomMath.NextFloat(engine.Random) < 0.75f)
                    {
                        surfaceBubbles[surfaceBubbleIndex].Y = bubbles[i].Position.Y;
                        surfaceBubbles[surfaceBubbleIndex].Frame = 0f;
                        surfaceBubbles[surfaceBubbleIndex].Animation =
                            (byte)engine.Random.Next(surfaceBubbleAnimations.Count);
                        surfaceBubbleIndex = (surfaceBubbleIndex + 1) % surfaceBubbles.Length;
                    }
                }
            }

            for (int j = 0; j < surfaceBubbles.Length; j++)
            {
                if (surfaceBubbles[j].Y >= 0f)
                {
                    surfaceBubbles[j].Frame += engine.DeltaTime * 6f;
                    if (surfaceBubbles[j].Frame >= (float)surfaceBubbleAnimations[surfaceBubbles[j].Animation].Count)
                    {
                        surfaceBubbles[j].Y = -1f;
                    }
                }
            }

            base.Update();
        }

        private Vector2 SourceLerp(Vector2 a, Vector2 b, float p) =>
            Vector2Interpolation.Lerp(a, b, p, engine.ScalarArithmetic);

        private float Sin(float value)
        {
            return (1f + (float)Math.Sin(value)) / 2f;
        }

        private float Wave(int step, float length)
        {
            int num = step * SurfaceStep;
            float num2 = Sin((float)num * 0.25f + timer * 4f) * SmallWaveAmplitude;
            num2 += Sin((float)num * 0.05f + timer * 0.5f) * BigWaveAmplitude;
            if (step % 2 == 0)
            {
                num2 += Spikey;
            }

            return num2 + (1f - ScalarMath.YoYo((float)num / length)) * CurveAmplitude;
        }

        private void Quad(ref int vert, Vector2 va, Vector2 vb, Vector2 vc, Vector2 vd, PackedColor color)
        {
            Quad(ref vert, va, color, vb, color, vc, color, vd, color);
        }

        private void Quad(ref int vert, Vector2 va, PackedColor ca, Vector2 vb, PackedColor cb, Vector2 vc,
            PackedColor cc, Vector2 vd, PackedColor cd)
        {
            verts[vert].Position.X = va.X;
            verts[vert].Position.Y = va.Y;
            verts[vert++].Color = ca;
            verts[vert].Position.X = vb.X;
            verts[vert].Position.Y = vb.Y;
            verts[vert++].Color = cb;
            verts[vert].Position.X = vc.X;
            verts[vert].Position.Y = vc.Y;
            verts[vert++].Color = cc;
            verts[vert].Position.X = va.X;
            verts[vert].Position.Y = va.Y;
            verts[vert++].Color = ca;
            verts[vert].Position.X = vc.X;
            verts[vert].Position.Y = vc.Y;
            verts[vert++].Color = cc;
            verts[vert].Position.X = vd.X;
            verts[vert].Position.Y = vd.Y;
            verts[vert++].Color = cd;
        }

        private void Edge(ref int vert, Vector2 a, Vector2 b, float fade)
        {
            Vector2 val = a - b;
            float num = engine.VectorArithmetic.Length(val);
            float num2 = num / (float)SurfaceStep;
            Vector2 val2 = Vector2Geometry.Perpendicular(engine.VectorArithmetic.SafeNormalize(b - a));
            for (int i = 1; (float)i <= num2; i++)
            {
                Vector2 val3 = SourceLerp(a, b, (float)(i - 1) / num2);
                float num3 = Wave(i - 1, num);
                Vector2 val4 = val3 - val2 * num3;
                Vector2 val5 = SourceLerp(a, b, (float)i / num2);
                float num4 = Wave(i, num);
                Vector2 val6 = val5 - val2 * num4;
                Quad(ref vert, val4 + val2, EdgeColor, val6 + val2, EdgeColor, val5 + val2 * (fade - num4), CenterColor,
                    val3 + val2 * (fade - num3), CenterColor);
                Quad(ref vert, val3 + val2 * (fade - num3), val5 + val2 * (fade - num4), val5 + val2 * fade,
                    val3 + val2 * fade, CenterColor);
                Quad(ref vert, val4, val6, val6 + val2, val4 + val2 * 1f, SurfaceColor);
            }
        }

        public override void Render(ISourceRenderContext context)
        {
            engine.GameplayBatchScope.End(context);
            if (dirty)
            {
                Vector2 zero = Vector2.Zero;
                Vector2 val = zero;
                Vector2 val2 = new Vector2(zero.X + Width, zero.Y);
                Vector2 val3 = new Vector2(zero.X, zero.Y + Height);
                Vector2 val4 = zero + new Vector2(Width, Height);
                Vector2 val5 = new Vector2(engine.ExtremaArithmetic.Min(Fade, Width / 2f),
                    engine.ExtremaArithmetic.Min(Fade, Height / 2f));
                vertCount = 0;
                if (OnlyMode == OnlyModes.OnlyLeft)
                {
                    Edge(ref vertCount, val2, val4, val5.X);
                    Quad(ref vertCount, val, val2 - new Vector2(val5.X, 0f), val4 - new Vector2(val5.X, 0f), val3,
                        CenterColor);
                }
                else if (OnlyMode == OnlyModes.OnlyRight)
                {
                    Edge(ref vertCount, val3, val, val5.X);
                    Quad(ref vertCount, val + new Vector2(val5.X, 0f), val2, val4, val3 + new Vector2(val5.X, 0f),
                        CenterColor);
                }

                dirty = false;
            }

            var camera = (base.Scene as ISourceCameraScene).Camera;
            SourcePrimitiveGraphics.DrawVertices(engine.PrimitiveGraphics,
                SourceMatrixMath.Multiply(
                    SourceMatrixMath.CreateTranslation(new Vector3(((ISourceEntityPosition)Entity).Position + Position,
                        0f)), camera.Matrix), verts, vertCount);
            engine.GameplayBatchScope.Begin(context);
            Vector2 val6 = ((ISourceEntityPosition)Entity).Position + Position;
            AtlasEntry val7 = engine.LiquidAtlas["particles/bubble"];
            for (int i = 0; i < bubbles.Length; i++)
            {
                SourceAtlasDrawing.DrawCentered(context, Entity, val7, val6 + bubbles[i].Position,
                    SurfaceColor * bubbles[i].Alpha);
            }

            for (int j = 0; j < surfaceBubbles.Length; j++)
            {
                if (surfaceBubbles[j].Y >= 0f)
                {
                    AtlasEntry obj = surfaceBubbleAnimations[surfaceBubbles[j].Animation][(int)surfaceBubbles[j].Frame];
                    int num = (int)(surfaceBubbles[j].Y / (float)SurfaceStep);
                    float num2 = ((OnlyMode != OnlyModes.OnlyLeft)
                        ? (1f - Wave(num, Height))
                        : (Width + 7f + Wave(num, Height)));
                    SourceAtlasDrawing.DrawJustified(context, Entity, obj,
                        val6 + new Vector2(num2,
                            (OnlyMode == OnlyModes.OnlyRight)
                                ? (Height - (float)(num * SurfaceStep))
                                : ((float)(num * SurfaceStep))), new Vector2(1f, 0.5f), SurfaceColor);
                }
            }
        }
    }
}
