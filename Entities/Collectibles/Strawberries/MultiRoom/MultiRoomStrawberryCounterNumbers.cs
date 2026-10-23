using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;

namespace Celeste.Mods.MaxHelpingHand.Entities.Collectibles.Strawberries.MultiRoom
{
    internal sealed class MultiRoomStrawberryCounterNumbers
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, MultiRoomStrawberryCounterNumbers> owners =
            new();

        internal AtlasEntry[] Numbers;

        internal static MultiRoomStrawberryCounterNumbers For(ISourceEngineContext engine)
            => owners.GetValue(engine, _ => new MultiRoomStrawberryCounterNumbers());

        internal void Initialize(ISourceEngineContext engine)
        {
            var source = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas["pico8/font"];
            Numbers = new AtlasEntry[11];
            int index = 0;
            for (int i = 104; index < 4; i += 4)
                Numbers[index++] = source.GetSubtexture(i, 0, 3, 5);
            for (int i = 0; index < 10; i += 4)
                Numbers[index++] = source.GetSubtexture(i, 6, 3, 5);
            Numbers[10] = source.GetSubtexture(24, 6, 3, 5);
        }
    }
}
