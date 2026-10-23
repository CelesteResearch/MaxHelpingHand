using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Scheduling;
using Celeste.Formats.Map.Binary.Source;
using Celeste.Everest.Content.Maps.Binary;
using Celeste.Game.Presentation.Backdrops.Blackhole;
using Celeste.Graphics.Content;
using Celeste.Graphics.Display.Colors;
using Celeste.Graphics.Content.Atlas;

namespace Celeste.Mods.MaxHelpingHand.Backdrops.Blackholes
{
    public partial class BlackholeCustomColors
    {
        public static BlackholeBackdropPresentation Create(ISourceEngineContext engine, BinaryPackerElement data)
        {
            float alphaInner = data.AttrFloat("bgAlphaInner", 1f);
            float alphaOuter = data.AttrFloat("bgAlphaOuter", 1f);
            if (data.HasAttr("bgAlpha"))
            {
                alphaInner = data.AttrFloat("bgAlpha", 1f);
                alphaOuter = data.AttrFloat("bgAlpha", 1f);
            }

            var atlas = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas;
            BlackholeBackdropPresentation result;
            if (!data.Attr("colorsMild").Contains("|") && !data.Attr("colorsWild").Contains("|")
                                                       && !data.Attr("bgColorInner").Contains("|") &&
                                                       !data.Attr("bgColorOuterMild").Contains("|")
                                                       && !data.Attr("bgColorOuterWild").Contains("|") &&
                                                       data.AttrBool("affectedByWind", true)
                                                       && data.AttrFloat("additionalWindX", 0f) == 0f &&
                                                       data.AttrFloat("additionalWindY", 0f) == 0f
                                                       && data.AttrFloat("fgAlpha", 1f) == 1f &&
                                                       string.IsNullOrEmpty(data.Attr("fadex"))
                                                       && string.IsNullOrEmpty(data.Attr("fadey")) &&
                                                       !data.AttrBool("invertedRendering")
                                                       && string.IsNullOrEmpty(data.Attr("particleTexture")))
            {
                var mild = BlackholeColorCycle.ParseColors(data.Attr("colorsMild", "6e3199,851f91,3026b0"));
                for (int i = 0; i < mild.Length; i++) mild[i] *= .8f;
                result = new BlackholeBackdropPresentation(engine, new BlackholeBackdropConstruction(mild));
                result.ApplyColors(new BlackholeBackdropPalette(mild,
                    BlackholeColorCycle.ParseColors(data.Attr("colorsWild", "ca4ca7,b14cca,ca4ca7")),
                    SourceHexColor.HexToColor(data.Attr("bgColorInner", "000000")) * alphaInner,
                    SourceHexColor.HexToColor(data.Attr("bgColorOuterMild", "512a8b")) * alphaOuter,
                    SourceHexColor.HexToColor(data.Attr("bgColorOuterWild", "bd2192")) * alphaOuter));
            }
            else
            {
                float fgAlpha = data.AttrFloat("fgAlpha", 1f);
                AtlasEntry texture = null;
                List<AtlasEntry> streams = null;
                if (!string.IsNullOrEmpty(data.Attr("particleTexture")))
                {
                    texture = atlas[data.Attr("particleTexture")];
                    streams = CutIntoSubtextures(texture, data.AttrInt("particleTextureCount"));
                }

                var mild = new BlackholeColorCycle(data.Attr("colorsMild", "6e3199,851f91,3026b0"),
                    .8f * fgAlpha, ((ISourceScalarEnvironment)engine).ScalarArithmetic).GetColors();
                var custom = new BlackholeCustomColors(engine, new BlackholeBackdropConstruction(mild, streams),
                    data.Attr("colorsMild", "6e3199,851f91,3026b0"), data.Attr("colorsWild", "ca4ca7,b14cca,ca4ca7"),
                    data.Attr("bgColorInner", "000000"), data.Attr("bgColorOuterMild", "512a8b"),
                    data.Attr("bgColorOuterWild", "bd2192"), alphaInner, alphaOuter, fgAlpha,
                    data.AttrBool("affectedByWind", true),
                    new Vector2(data.AttrFloat("additionalWindX", 0f), data.AttrFloat("additionalWindY", 0f)),
                    data.AttrBool("invertedRendering"), texture);
                custom.ApplyColors(new BlackholeBackdropPalette(mild, custom.cycleColorsWild.GetColors(),
                    custom.cycleBgColorInner.GetColors()[0], custom.cycleBgColorOuterMild.GetColors()[0],
                    custom.cycleBgColorOuterWild.GetColors()[0]));
                result = custom;
            }

            result.Alpha = data.AttrFloat("alpha", 1f);
            result.Direction = data.AttrFloat("direction", 1f);
            if (!string.IsNullOrEmpty(data.Attr("texture"))) result.SetBackgroundTexture(atlas[data.Attr("texture")]);
            return result;
        }

        private static List<AtlasEntry> CutIntoSubtextures(AtlasEntry texture, int count)
        {
            var result = new List<AtlasEntry>();
            int width = texture.FrameWidth / count;
            int x = 0;
            for (int i = 0; i < count; i++)
            {
                result.Add(new AtlasEntry(texture, x, 0, width, texture.FrameHeight));
                x += width;
            }

            return result;
        }
    }
}
