using System;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Everest.Content.Maps.Binary;
using Celeste.Everest.Runtime.Levels.Update;
using Celeste.Formats.Map.Binary.Source;
using Celeste.Game.Content.Maps;
using Celeste.Graphics.Display.Colors;
using Celeste.Mods.MaxHelpingHand.Backdrops.Stars;
using Celeste.Simulation.Presentation.Backdrops.Runtime;

namespace Celeste.Mods.MaxHelpingHand.Runtime.Backdrops
{
    public sealed class CustomStarsRegistration : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly MaxBackdropBufferState buffers;

        public CustomStarsRegistration(ISourceEngineContext engine)
        {
            this.engine = engine;
            buffers = new MaxBackdropBufferState(engine);
            ((IEverestLevelEntityLoadEnvironment)engine).LevelUpdateEvents.OnLoadBackdrop += Load;
        }

        public BackdropPresentation Load(OriginalChapterMapData map, BinaryPackerElement child,
            BinaryPackerElement above)
        {
            if (!child.Name.Equals("MaxHelpingHand/CustomStars", StringComparison.OrdinalIgnoreCase)) return null;
            int? count = int.TryParse(child.Attr("starCount", ""), out int value) ? value : null;
            string tint = child.Attr("tint", "");
            if (child.AttrBool("disableTinting", false)) tint = "ffffff";
            float? alpha = null;
            string text = child.Attr("starAlpha", "");
            if (!string.IsNullOrEmpty(text)) alpha = float.Parse(text);
            Vector2? scroll = null;
            if (!string.IsNullOrEmpty(child.Attr("scrollX", "")) && !string.IsNullOrEmpty(child.Attr("scrollY", "")))
                scroll = new Vector2(float.Parse(child.Attr("scrollX", "")), float.Parse(child.Attr("scrollY", "")));
            return new CustomStars(engine, buffers, count,
                string.IsNullOrEmpty(tint) ? null : SourceHexColor.HexToColor(tint),
                child.Attr("spriteDirectory", "bgs/02/stars"), child.AttrFloat("wrapHeight", 180f),
                child.AttrFloat("width", 320f),
                alpha, child.AttrFloat("bgAlpha", 1f), scroll);
        }

        public void Dispose()
        {
            ((IEverestLevelEntityLoadEnvironment)engine).LevelUpdateEvents.OnLoadBackdrop -= Load;
            buffers.Dispose();
        }
    }
}
