// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using Celeste.Core.Maps;

namespace Celeste.Mods.MaxHelpingHand.Content.Maps
{
    internal sealed class FlagSwitchMapGroups
    {
        internal readonly Dictionary<KeyValuePair<string, bool>, List<MapEntityId>> Switches = new();
        internal readonly Dictionary<string, Dictionary<MapEntityId, bool>> Gates = new();
        internal readonly Dictionary<KeyValuePair<string, bool>, bool> Persistences = new();
        internal readonly Dictionary<KeyValuePair<string, bool>, bool> LegacyModes = new();
    }
}
