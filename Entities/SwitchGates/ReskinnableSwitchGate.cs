// SPDX-License-Identifier: MIT

using System.Numerics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Tracking.Attributes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Mechanisms.SwitchGates;
using Celeste.Graphics.Display.Colors;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.MaxHelpingHand.Entities.SwitchGates
{
    [CustomEntity("MaxHelpingHand/ReskinnableSwitchGate")]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.ReskinnableSwitchGate")]
    [EverestTrackedAs(typeof(OriginalSwitchGateState))]
    public class ReskinnableSwitchGate : OriginalSwitchGateState
    {
        public static ReskinnableSwitchGate Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
        {
            string iconPath = data.Attr("icon", "");
            return new ReskinnableSwitchGate(data, offset, (IOriginalSwitchGateProcess)level.EngineContext,
                ((ISourcePlatformCollisionScene)level).SolidWorldFactory, iconPath);
        }

        public ReskinnableSwitchGate(EntityData data, Vector2 offset, IOriginalSwitchGateProcess process,
            ISourceSolidWorldFactory worlds)
            : this(data, offset, process, worlds, "objects/switchgate/icon")
        {
        }

        private ReskinnableSwitchGate(EntityData data, Vector2 offset, IOriginalSwitchGateProcess process,
            ISourceSolidWorldFactory worlds, string iconPath)
            : base(data.Position + offset, data.Width, data.Height, data.Nodes[0] + offset,
                data.Bool("persistent"), data.Attr("sprite", "block"), process, worlds, iconPath)
        {
            SurfaceSoundIndex = data.Int("surfaceIndex", SurfaceSoundIndex);
            inactiveColor = SourceHexColor.HexToColor(data.Attr("inactiveColor", ""));
            activeColor = SourceHexColor.HexToColor(data.Attr("activeColor", ""));
            finishColor = SourceHexColor.HexToColor(data.Attr("finishColor", ""));
            icon.Color = inactiveColor;
        }
    }
}
