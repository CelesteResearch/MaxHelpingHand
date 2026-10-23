using Celeste.Core.Scheduling;
using Celeste.Core.Randomness;
using Celeste.Core.Display;
using Celeste.Core.Rendering;
using Celeste.Core.Reflection.Runtime;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Display;
using Celeste.Graphics.Rendering;
using Celeste.Simulation.Presentation.Backdrops.Runtime;
using Celeste.Mods.MaxHelpingHand.Backdrops.Foundation;
using Celeste.Mods.MaxHelpingHand.Runtime.Backdrops;
using System.Numerics;
using Color = Celeste.Core.Display.PackedColor;
using MTexture = Celeste.Graphics.Content.Atlas.AtlasEntry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Celeste.Mods.MaxHelpingHand.Backdrops.Stars
{
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Effects.CustomStars")]
    public class CustomStars : CustomBackdrop
    {
        private struct Star
        {
            public Vector2 Position;
            public int TextureSet;
            public float Timer;
            public float Rate;
        }

        private readonly ISourceEngineContext engine;
        private readonly MaxBackdropBufferState buffers;
        private ISourceRandom Random => ((ISourceRandomEnvironment)engine).Random;
        private int? starCount;
        private Color? tint;
        private float wrapHeight;
        private float? starAlpha;
        private float bgAlpha;

        private Star[] stars;
        private Color[] colors;

        private List<List<MTexture>> textures;

        private float falling = 0f;
        private Vector2 center;
        private Vector2? scroll;

        public CustomStars(ISourceEngineContext engine, MaxBackdropBufferState buffers, int? starCount, Color? tint,
            string spriteDirectory, float wrapHeight, float width, float? starAlpha, float bgAlpha, Vector2? scroll)
        {
            this.engine = engine;
            this.buffers = buffers;
            this.starCount = starCount;
            this.tint = tint;
            this.wrapHeight = wrapHeight;
            this.starAlpha = starAlpha;
            this.bgAlpha = bgAlpha;
            this.scroll = scroll;

            // look up all the stars in the folder, and group frames belonging to the same stars.
            textures = new List<List<MTexture>>();
            string pathPrefix = spriteDirectory + "/";
            List<MTexture> folderContents = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas.Entries.Keys
                .Where(path => path.StartsWith(pathPrefix))
                .Select(path => ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas[path]).ToList();
            Dictionary<string, List<MTexture>> starList = new Dictionary<string, List<MTexture>>();
            foreach (MTexture texture in folderContents)
            {
                string starName = Regex.Replace(texture.Name, "\\d+$", string.Empty);
                if (!starList.TryGetValue(starName, out List<MTexture> starTextures))
                {
                    starTextures = new List<MTexture>();
                    starList[starName] = starTextures;
                }

                starTextures.Add(texture);
            }

            // be sure that the star sprites are ordered.
            foreach (List<MTexture> list in starList.Values)
            {
                list.Sort((a, b) => a.Name.CompareTo(b.Name));
            }

            // then add tlem to the texture list for the game to use them.
            textures.AddRange(starList.Values);

            center = new Vector2(textures[0][0].FrameWidth, textures[0][0].FrameHeight) / 2f;
            stars = new Star[starCount ?? 100];
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i] = new Star
                {
                    Position = new Vector2(SourceRandomMath.NextFloat(Random, width),
                        SourceRandomMath.NextFloat(Random, wrapHeight)),
                    Timer = SourceRandomMath.NextFloat(Random, (float)Math.PI * 2f),
                    Rate = 2f + SourceRandomMath.NextFloat(Random, 2f),
                    TextureSet = Random.Next(textures.Count)
                };
            }

            colors = new Color[8];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = (tint ?? Color.Teal) * (starAlpha ?? 0.7f) * (1f - (float)i / colors.Length);
            }
        }

        public override void Update(ISourceBackdropScene scene)
        {
            base.Update(scene);
            if (Visible)
            {
                var level = scene;
                for (int i = 0; i < stars.Length; i++)
                {
                    stars[i].Timer += engine.DeltaTime * stars[i].Rate;
                }

                if (level.Dreaming)
                {
                    falling += engine.DeltaTime * 12f;
                }
            }
        }

        public override void Render(ISourceRenderContext context, ISourceBackdropScene scene)
        {
            float fadeAlpha = GetFadeAlpha(scene);

            Celeste.Graphics.Rendering.SourcePrimitiveDrawing.Rect(context, null, 0f, 0f, buffers.Width, buffers.Height,
                Color.Black * bgAlpha);
            var level = scene;
            Color color = (tint * (starAlpha ?? 1f)) ??
                          (level.Dreaming ? Color.Teal * (starAlpha ?? 0.7f) : Color.White);
            int count = starCount ?? (level.Dreaming ? 100 : 50);
            for (int i = 0; i < count; i++)
            {
                List<MTexture> starTextures = textures[stars[i].TextureSet];
                int starFrame = (int)((Math.Sin(stars[i].Timer) + 1.0) / 2.0 * starTextures.Count);
                starFrame %= starTextures.Count;
                Vector2 position = stars[i].Position;
                MTexture frameToRender = starTextures[starFrame];

                Vector2 effectiveScroll = scroll ?? (level.Dreaming ? Vector2.UnitY : Vector2.Zero);

                // parallax X
                position.X -= level.Camera.X * effectiveScroll.X;
                position.X %= buffers.Width;
                if (position.X < 0f)
                {
                    position.X += buffers.Width;
                }

                // parallax Y
                position.Y -= level.Camera.Y * effectiveScroll.Y;
                if (level.Dreaming) position.Y += falling * stars[i].Rate;
                position.Y %= wrapHeight;
                if (position.Y < 0f)
                {
                    position.Y += wrapHeight;
                }

                position.Y -= (wrapHeight - buffers.Height) / 2;

                if (level.Dreaming)
                {
                    for (int j = 0; j < colors.Length; j++)
                    {
                        var trail = new AtlasTextureDraw(frameToRender, position - Vector2.UnitY * j, center,
                            colors[j] * fadeAlpha, Vector2.One);
                        ((ISourceRenderCommandSink<AtlasTextureDraw>)context).Draw(in trail, null);
                    }
                }

                var draw = new AtlasTextureDraw(frameToRender, position, center, color * fadeAlpha, Vector2.One);
                ((ISourceRenderCommandSink<AtlasTextureDraw>)context).Draw(in draw, null);
            }
        }
    }
}
