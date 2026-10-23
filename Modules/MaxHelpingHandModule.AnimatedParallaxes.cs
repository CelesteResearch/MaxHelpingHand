// SPDX-License-Identifier: MIT

using System;
using System.Reflection;
using Celeste.Everest.Modules.Persistence;
using Celeste.Game.Content.Maps.Backdrops;
using Celeste.Mods.MaxHelpingHand.Backdrops;
using Celeste.Mods.MaxHelpingHand.Sessions;
using Celeste.Simulation.Presentation.Backdrops.Parallax;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Modules
{
    public sealed partial class MaxHelpingHandModule
    {
        private static readonly FieldInfo ParserEngine = typeof(MapBackdropParser)
            .GetField("engine", BindingFlags.NonPublic | BindingFlags.Instance);

        public override EverestModuleSession _Session
        {
            get => base._Session;
            set
            {
                base._Session = value;
                if (value is MaxHelpingHandSession session) session.BindAmbienceOwner(SourceEngine);
            }
        }

        private void LoadAnimatedParallaxes()
        {
            hooks.Add(new ILHook(typeof(MapBackdropParser).GetMethod(nameof(MapBackdropParser.ParseBackdrop)), il =>
            {
                var cursor = new ILCursor(il);
                while (cursor.TryGotoNext(MoveType.After,
                           instruction => instruction.MatchNewobj<ParallaxBackdropPresentation>()))
                {
                    cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                    cursor.EmitDelegate<Func<ParallaxBackdropPresentation, MapBackdropParser,
                        ParallaxBackdropPresentation>>(HandleAnimatedParallax);
                }
            }));
        }

        private ParallaxBackdropPresentation HandleAnimatedParallax(ParallaxBackdropPresentation original,
            MapBackdropParser parser)
        {
            if (ReferenceEquals(ParserEngine.GetValue(parser), SourceEngine) &&
                original.Texture?.AtlasPath?.StartsWith("bgs/MaxHelpingHand/animatedParallax/") == true)
                return new AnimatedParallax(SourceEngine, original.Texture);
            return original;
        }
    }
}
