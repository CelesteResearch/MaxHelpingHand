using System;
using Celeste.Everest;
using System.Collections.Generic;
using Celeste.Core.Maps;
using Celeste.Everest.Content.Maps.Binary;
using Celeste.Core.Scheduling;
using Celeste.Everest.Diagnostics.Logging;
using Celeste.Everest.Content.Maps.Processing;
using Celeste.Formats.Map.Binary.Source;

namespace Celeste.Mods.MaxHelpingHand.Content.Maps
{
    internal sealed class MultiRoomStrawberryMapDataProcessor : EverestMapDataProcessor
    {
        private readonly ISourceEngineContext engine;

        public MultiRoomStrawberryMapDataProcessor(ISourceEngineContext engine)
        {
            this.engine = engine;
        }

        private string levelName;
        private readonly Dictionary<string, List<BinaryPackerElement>> seedsByName = new();
        private readonly Dictionary<string, MapEntityId> idsByName = new();
        private readonly Dictionary<string, BinaryPackerElement> berriesByName = new();

        public override Dictionary<string, Action<BinaryPackerElement>> Init() => new()
        {
            ["level"] = level =>
            {
                levelName = level.Attr("name").Split(':')[0];
                if (levelName.StartsWith("lvl_")) levelName = levelName.Substring(4);
            },
            ["entity:MaxHelpingHand/MultiRoomStrawberrySeed"] = seed =>
            {
                string name = seed.Attr("strawberryName");
                if (seedsByName.ContainsKey(name))
                {
                    if (seed.AttrInt("index") < 0) seed.SetAttr("index", seedsByName[name].Count);
                    seedsByName[name].Add(seed);
                }
                else
                {
                    if (seed.AttrInt("index") < 0) seed.SetAttr("index", 0);
                    seedsByName[name] = new List<BinaryPackerElement> { seed };
                }
            },
            ["entity:MaxHelpingHand/MultiRoomStrawberry"] = berry =>
            {
                string name = berry.Attr("name");
                idsByName[name] = new MapEntityId(levelName, berry.AttrInt("id"));
                berriesByName[name] = berry;
            }
        };

        public override void Reset()
        {
        }

        public override void End()
        {
            foreach (string name in seedsByName.Keys)
            {
                if (!idsByName.ContainsKey(name))
                {
                    ((IEverestLoggerProcessEnvironment)engine).Logger.Log(EverestLogLevel.Warn,
                        "MaxHelpingHandMapDataProcessor",
                        "Multi-room strawberry seeds with name " + name + " didn't match any multi-room strawberry");
                }
                else
                {
                    var id = idsByName[name];
                    foreach (var seed in seedsByName[name])
                    {
                        seed.SetAttr("berryLevel", id.Level);
                        seed.SetAttr("berryID", id.ID);
                        seed.SetAttr("seedCount", seedsByName[name].Count);
                    }

                    berriesByName[name].SetAttr("seedCount", seedsByName[name].Count);
                }
            }

            seedsByName.Clear();
            idsByName.Clear();
            berriesByName.Clear();
            levelName = null;
        }
    }
}
