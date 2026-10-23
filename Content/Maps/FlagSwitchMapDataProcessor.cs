// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using Celeste.Core.Maps;
using Celeste.Everest.Content.Maps.Binary;
using Celeste.Everest.Content.Maps.Processing;
using Celeste.Formats.Map.Binary.Source;
using Celeste.Mods.MaxHelpingHand.Runtime.FlagSwitches;

namespace Celeste.Mods.MaxHelpingHand.Content.Maps
{
    internal sealed class FlagSwitchMapDataProcessor : EverestMapDataProcessor
    {
        private readonly FlagSwitchGroups registry;
        private FlagSwitchMapGroups groups;
        private string levelName;

        public FlagSwitchMapDataProcessor(FlagSwitchGroups registry)
        {
            this.registry = registry;
        }

        public override void Reset()
        {
            groups = new();
            registry.Set(MapData, groups);
        }

        public override void End()
        {
            levelName = null;
        }

        public override Dictionary<string, Action<BinaryPackerElement>> Init() => new()
        {
            ["level"] = level =>
            {
                levelName = level.Attr("name").Split(':')[0];
                if (levelName.StartsWith("lvl_")) levelName = levelName.Substring(4);
            },
            ["entity:MaxHelpingHand/RegularJumpThru"] = data => data.Name = "jumpThru",
            ["entity:MaxHelpingHand/FlagTouchSwitch"] = AddSwitch,
            ["entity:MaxHelpingHand/MovingFlagTouchSwitch"] = AddSwitch,
            ["entity:MaxHelpingHand/FlagTouchSwitchWall"] = AddSwitch,
            ["entity:ChroniaHelper/FlagTouchSwitch"] = data =>
            {
                if (data.Attr("switch") == "touchSwitch")
                {
                    data.Name = "MaxHelpingHand/FlagTouchSwitch";
                    data.SetAttr("x", data.AttrInt("x") + data.AttrInt("width") / 2);
                    data.SetAttr("y", data.AttrInt("y") + data.AttrInt("height") / 2);
                }
                else data.Name = "MaxHelpingHand/FlagTouchSwitchWall";

                AddSwitch(data);
            },
            ["entity:MaxHelpingHand/FlagSwitchGate"] = AddGate,
            ["entity:ChroniaHelper/FlagSwitchGate"] = data =>
            {
                data.Name = "MaxHelpingHand/FlagSwitchGate";
                AddGate(data);
            },
            ["entity:CommunalHelper/MaxHelpingHand/DreamFlagSwitchGate"] = AddGate,
            ["entity:MaxHelpingHand/ShatterFlagSwitchGate"] = data =>
            {
                data.Name = "MaxHelpingHand/FlagSwitchGate";
                data.SetAttr("isShatter", true);
                AddGate(data);
            }
        };

        private void AddSwitch(BinaryPackerElement data)
        {
            var key = new KeyValuePair<string, bool>(data.Attr("flag"), data.AttrBool("inverted"));
            if (!groups.Switches.TryGetValue(key, out var ids)) groups.Switches.Add(key, ids = new());
            ids.Add(new MapEntityId(levelName, data.AttrInt("id")));
            Merge(groups.Persistences, key, data.AttrBool("persistent", true));
            Merge(groups.LegacyModes, key, data.AttrBool("legacyFlagMode", true));
        }

        private static void Merge(Dictionary<KeyValuePair<string, bool>, bool> values, KeyValuePair<string, bool> key,
            bool value)
        {
            values[key] = (!values.TryGetValue(key, out bool previous) || previous) && value;
        }

        private void AddGate(BinaryPackerElement data)
        {
            string flag = data.Attr("flag");
            if (!groups.Gates.TryGetValue(flag, out var gates)) groups.Gates.Add(flag, gates = new());
            gates.Add(new MapEntityId(levelName, data.AttrInt("id")), data.AttrBool("persistent"));
        }
    }
}
