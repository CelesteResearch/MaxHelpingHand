// SPDX-License-Identifier: MIT

using System;
using System.Linq;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Everest.Modules.Updating;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Display.Colors;
using Celeste.Mods.MaxHelpingHand.Modules;
using Celeste.Mods.MaxHelpingHand.Runtime.FlagSwitches;
using Celeste.Game.Gameplay.Mechanisms.Switches;
using Celeste.Scene.Entities;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.MaxHelpingHand.Entities.Switches
{
    [CustomEntity("MaxHelpingHand/MovingFlagTouchSwitch")]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.MovingFlagTouchSwitch")]
    public static class MovingFlagTouchSwitch
    {
        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data)
        {
            var engine = level.EngineContext;
            if (!((IEverestLoaderProcessEnvironment)engine).Loader.DependencyLoaded(new EverestModuleMetadata
                    { Name = "OutbackHelper", Version = new Version(1, 4, 0) }))
                return new MovingFlagTouchSwitchErrorSpawner(engine);
            var module = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<MaxHelpingHandModule>().Single();
            string flag = data.Attr("flag", "");
            module.FlagSwitchGroups.ResetFlagIfNecessary(level, flag, false);
            if (level.Session.GetFlag(flag) || level.Session.GetFlag(flag + "_switch" + data.ID))
            {
                var position = data.Position;
                if (data.Nodes.Length != 0) data.Position = data.Nodes[data.Nodes.Length - 1];
                var result = new FlagTouchSwitch(engine, data, offset);
                data.Position = position;
                return result;
            }

            var binding = module.MovingFlagTouchSwitches.Binding;
            var moving = binding.Create(engine, data, offset);
            binding.SetMotion(moving, SourceHexColor.HexToColor(data.Attr("movingColor", "FF8080")),
                data.Float("movingDelay", .8f));
            var inactive = SourceHexColor.HexToColor(data.Attr("inactiveColor", "5FCDE4"));
            var active = SourceHexColor.HexToColor(data.Attr("activeColor", "FFFFFF"));
            var finish = SourceHexColor.HexToColor(data.Attr("finishColor", "F141DF"));
            var process = (Celeste.Game.Gameplay.Mechanisms.Switches.IOriginalTouchSwitchProcess)engine;
            var fire = new ParticleTypePresentation(process.ParticleTypes.TouchSwitch.P_Fire) { Color = finish };
            string iconName = data.Attr("icon", "vanilla");
            var icon = new Sprite(engine, process.GameGraphics.GameAtlas,
                iconName == "vanilla"
                    ? "objects/touchswitch/icon"
                    : "objects/MaxHelpingHand/flagTouchSwitch/" + iconName + "/icon");
            icon.Add("idle", "", 0f, 0);
            icon.Add("spin", "", .1f, new Celeste.Core.Randomness.Chooser<string>("spin", 1f), 0, 1, 2, 3, 4, 5);
            icon.Play("spin");
            icon.Color = inactive;
            icon.CenterOrigin();
            binding.ConfigureAppearance(moving, inactive, active, finish, icon, fire);
            string hidden = data.Attr("hideIfFlag", "");
            var attachment = new MovingFlagTouchSwitchAttachment(module.FlagSwitchGroups, moving, flag,
                data.Bool("persistent", false), data.ID, hidden);
            module.MovingFlagTouchSwitches.Register(moving, attachment);
            moving.Add(attachment);
            if (!string.IsNullOrEmpty(hidden)) moving.Add(new MovingFlagTouchSwitchHiddenListener(engine, hidden));
            return moving;
        }
    }
}
