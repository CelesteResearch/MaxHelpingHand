// SPDX-License-Identifier: MIT

using Celeste.Core.Display;
using System;
using Celeste.Core.Reflection.Runtime;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Randomness;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Display;
using Celeste.Graphics.Display.Colors;
using Celeste.Simulation.Presentation.Backdrops.Runtime;
using Celeste.Simulation.Presentation.Backdrops.Stars;

namespace Celeste.Mods.MaxHelpingHand.Backdrops.Stars
{
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Effects.CustomStarfield")]
    public class CustomStarfield : StarfieldBackdropPresentation
    {
        private static readonly Regex prefixesPattern = new Regex("^([0-9]+)_[0-9]+$");
        private static Regex capturePrefixes() => prefixesPattern;
        private readonly ISourceEngineContext engine;

        private float fps;
        private AtlasEntry[][] starAnimations;
        private float[] frameOffsets;

        public CustomStarfield(ISourceEngineContext engine, string[] paths, PackedColor[] colors, float[] alphas,
            bool shuffle = true, float speed = 1f, float fps = 0)
            : base(engine, colors[0], speed)
        {
            this.engine = engine;
            this.fps = fps;
            var atlas = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas;
            var random = ((ISourceRandomEnvironment)engine).Random;
            starAnimations = new AtlasEntry[Stars.Length][];
            frameOffsets = new float[Stars.Length];

            AtlasEntry[][][] textures = new AtlasEntry[paths.Length][][];
            for (int i = 0; i < paths.Length; i++)
            {
                if (fps <= 0)
                {
                    // single frame for each star: [path][index] => GetAtlasSubtextures captures the index
                    List<AtlasEntry> atlasSubtextures = atlas.Frames("particles/" + paths[i] + "/");
                    textures[i] = new AtlasEntry[atlasSubtextures.Count][];
                    for (int j = 0; j < atlasSubtextures.Count; j++)
                    {
                        textures[i][j] = new AtlasEntry[] { atlasSubtextures[j] };
                    }
                }
                else
                {
                    // animations for each star: [path][index]_[framenumber] => GetAtlasSubtextures captures the framenumber
                    string basePath = "particles/" + paths[i] + "/";
                    List<string> prefixes = atlas.Entries.Keys
                        .Where(path => path.StartsWith(basePath))
                        .Select(path =>
                        {
                            // [path][index]_[framenumber] => [index]_[framenumber] => the regex captures [index]
                            Match capture = capturePrefixes().Match(path.Substring(basePath.Length));
                            if (!capture.Success) return null; // filtered out
                            return basePath + capture.Groups[1] + "_";
                        })
                        .Where(path => path != null)
                        .OrderBy(path => path).Distinct()
                        .ToList();

                    textures[i] = new AtlasEntry[prefixes.Count][];
                    for (int j = 0; j < prefixes.Count; j++)
                    {
                        textures[i][j] = atlas.Frames(prefixes[j]).ToArray();
                    }
                }
            }

            int textureIndex = 0;
            int colorIndex = 0;
            int alphaIndex = 0;

            for (int i = 0; i < Stars.Length; i++)
            {
                if (shuffle)
                {
                    textureIndex = random.Next(textures.Length);
                    colorIndex = random.Next(colors.Length);
                    alphaIndex = random.Next(alphas.Length);
                }

                AtlasEntry[][] atlasSubtextures = textures[textureIndex];
                PackedColor blended = colors[colorIndex] * alphas[alphaIndex];

                // Distance, Color and texture index are computed from the same random number.
                // We are reversing the operation (Distance = 4 + randomNumber * 20) to find out the random number and use it to set the color and texture again.
                float randomNumber = (Stars[i].Distance - 4) / 20f;

                Stars[i].Color = SourceColorInterpolation.Lerp(blended, PackedColor.Transparent, randomNumber * 0.5f,
                    ((ISourceScalarEnvironment)engine).ScalarArithmetic);
                int texIndex =
                    (int)ScalarMath.Clamp(
                        ((ISourceEasingEnvironment)engine).EasingArithmetic.CubeIn(1f - randomNumber) *
                        atlasSubtextures.Length, 0f, atlasSubtextures.Length - 1);
                Stars[i].Texture = atlasSubtextures[texIndex][0];
                starAnimations[i] = atlasSubtextures[texIndex];
                if (starAnimations[i].Length > 1)
                {
                    frameOffsets[i] = random.NextSingle() * starAnimations[i].Length;
                }

                if (!shuffle)
                {
                    textureIndex = (textureIndex + 1) % textures.Length;
                    colorIndex = (colorIndex + 1) % colors.Length;
                    alphaIndex = (alphaIndex + 1) % alphas.Length;
                }
            }
        }

        public override void Update(ISourceBackdropScene scene)
        {
            base.Update(scene);
            if (fps <= 0) return;

            for (int i = 0; i < Stars.Length; i++)
            {
                int currentFrame = ((int)(scene.SourceScene.Clock.TimeActive * fps + frameOffsets[i])) %
                                   starAnimations[i].Length;
                Stars[i].Texture = starAnimations[i][currentFrame];
            }
        }

        private float GetFadeAlpha(ISourceBackdropScene scene)
        {
            float alpha = 1f;
            Vector2 camera = scene.Camera.Position;
            camera = new Vector2((float)Math.Floor(camera.X), (float)Math.Floor(camera.Y));
            if (FadeX != null) alpha *= FadeX.Value(camera.X + 160f);
            if (FadeY != null) alpha *= FadeY.Value(camera.Y + 90f);
            return alpha;
        }

        public override void Render(ISourceRenderContext context, ISourceBackdropScene scene)
        {
            float origFadeAlpha = FadeAlphaMultiplier;
            FadeAlphaMultiplier *= GetFadeAlpha(scene);

            base.Render(context, scene);

            FadeAlphaMultiplier = origFadeAlpha;
        }
    }
}
