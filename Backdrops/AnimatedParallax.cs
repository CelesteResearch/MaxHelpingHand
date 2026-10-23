// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Core.Text;
using Celeste.Everest.Content.Registry;
using Celeste.Everest.Content.Serialization;
using Celeste.Everest.Content.Types.Text;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Simulation.Presentation.Backdrops.Parallax;
using Celeste.Simulation.Presentation.Backdrops.Runtime;

namespace Celeste.Mods.MaxHelpingHand.Backdrops
{
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Effects.AnimatedParallax")]
    public class AnimatedParallax : ParallaxBackdropPresentation
    {
        private readonly List<AtlasEntry> frames;
        private readonly int[] frameOrder;
        private readonly float fps;
        private int currentFrame;
        private float currentFrameTimer;

        public AnimatedParallax(ISourceEngineContext engine, AtlasEntry texture) : base(texture)
        {
            string texturePath = Regex.Replace(texture.AtlasPath, "\\d+$", string.Empty);
            frames = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas.GetAtlasSubtextures(texturePath);
            frameOrder = new int[frames.Count];
            for (int i = 0; i < frameOrder.Length; i++) frameOrder[i] = i;
            Match fpsCount = Regex.Match(texturePath, "[^0-9]((?:[0-9]+\\.)?[0-9]+)fps$");
            fps = fpsCount.Success ? float.Parse(fpsCount.Groups[1].Value) : 12f;
            if (((IEverestContentRegistryEnvironment)engine).SourceContentRegistry.Map.TryGetValue(
                    "Graphics/Atlases/Gameplay/" + texturePath + ".meta", out var metaYaml) &&
                metaYaml.Type == typeof(AssetTypeYaml))
            {
                ParallaxAnimationMetadata meta;
                using (TextReader reader = new StreamReader(metaYaml.Stream))
                    meta = ((IEverestYamlEnvironment)engine).Yaml.Deserializer
                        .Deserialize<ParallaxAnimationMetadata>(reader);
                if (meta.FPS != null) fps = meta.FPS.Value;
                if (meta.Frames != null) frameOrder = SourceCsvIntegerFrames.Read(meta.Frames);
            }

            Texture = frames[frameOrder[0]];
            currentFrame = 0;
            currentFrameTimer = 1f / fps;
        }

        public override void Update(ISourceBackdropScene scene)
        {
            base.Update(scene);
            if (IsVisible(scene))
            {
                currentFrameTimer -= scene.EngineContext.DeltaTime;
                if (currentFrameTimer < 0f)
                {
                    currentFrameTimer += 1f / fps;
                    currentFrame++;
                    currentFrame %= frameOrder.Length;
                    Texture = frames[frameOrder[currentFrame]];
                }
            }
        }
    }
}
