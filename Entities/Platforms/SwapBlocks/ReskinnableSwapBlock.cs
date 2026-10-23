using System.Numerics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Tracking.Attributes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Entities.Platforms.SwapBlocks;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Graphics.Content;
using Celeste.Graphics.Display.Colors;
using Celeste.Mods.MaxHelpingHand.Runtime.SwapBlocks;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.MaxHelpingHand.Entities.Platforms.SwapBlocks
{
    [CustomEntity("MaxHelpingHand/ReskinnableSwapBlock = Create")]
    [EverestTrackedAs(typeof(OriginalSwapBlockState))]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.ReskinnableSwapBlock")]
    public class ReskinnableSwapBlock : OriginalSwapBlockState
    {
        private readonly string spriteDirectory;
        private readonly ParticleTypePresentation customParticleColor;
        private readonly string moveSound;
        private readonly string moveEndSound;
        private readonly string returnSound;
        private readonly string returnEndSound;
        private readonly bool transparentBackground;

        public static ReskinnableSwapBlock Create(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
            => new(level.EngineContext, data, offset, level.SolidWorldFactory.Create(data.Position + offset, false));

        public ReskinnableSwapBlock(ISourceEngineContext engine, EntityData data, Vector2 offset,
            ISourceSolidWorld world)
            : base(engine, data, offset, world,
                ReskinnableSwapBlockBinding.IsLoaded(engine) ? data.Attr("spriteDirectory", "objects/swapblock") : null,
                ReskinnableSwapBlockBinding.IsLoaded(engine)
                    ? data.Bool("transparentBackground")
                        ? "MaxHelpingHand/swapblocktransparentbg/path"
                        : data.Attr("spriteDirectory", "objects/swapblock") + "/path"
                    : null)
        {
            spriteDirectory = data.Attr("spriteDirectory", "objects/swapblock");
            transparentBackground = data.Bool("transparentBackground");
            var atlas = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas;
            SourceMiddleGreen.Reset(atlas, spriteDirectory + "/");
            SourceMiddleGreen.AddLoop("idle", "midBlock", .08f);
            SourceMiddleGreen.Play("idle");
            SourceMiddleRed.Reset(atlas, spriteDirectory + "/");
            SourceMiddleRed.AddLoop("idle", "midBlockRed", .08f);
            SourceMiddleRed.Play("idle");
            customParticleColor =
                new ParticleTypePresentation(((IOriginalGameplayProcess)engine).ParticleTypes.SwapBlock.P_Move)
                {
                    Color = SourceHexColor.HexToColor(data.Attr("particleColor1", "fbf236")),
                    Color2 = SourceHexColor.HexToColor(data.Attr("particleColor2", "6abe30"))
                };
            moveSound = data.Attr("moveSound", "event:/game/05_mirror_temple/swapblock_move");
            moveEndSound = data.Attr("moveEndSound", "event:/game/05_mirror_temple/swapblock_move_end");
            returnSound = data.Attr("returnSound", "event:/game/05_mirror_temple/swapblock_return");
            returnEndSound = data.Attr("returnEndSound", "event:/game/05_mirror_temple/swapblock_return_end");
        }

        protected override string ResolvePathTexturePrefix(string prefix)
            => ReskinnableSwapBlockBinding.IsLoaded(EngineContext)
                ? transparentBackground
                    ? "MaxHelpingHand/swapblocktransparentbg/path"
                    : prefix.Replace("objects/swapblock", spriteDirectory)
                : prefix;

        protected override string ResolveSoundPath(string path)
        {
            if (!ReskinnableSwapBlockBinding.IsLoaded(EngineContext)) return path;
            return path switch
            {
                "event:/game/05_mirror_temple/swapblock_move" => moveSound,
                "event:/game/05_mirror_temple/swapblock_move_end" => moveEndSound,
                "event:/game/05_mirror_temple/swapblock_return" => returnSound,
                "event:/game/05_mirror_temple/swapblock_return_end" => returnEndSound,
                _ => path
            };
        }

        public override void Update()
        {
            var particles = ((IOriginalGameplayProcess)EngineContext).ParticleTypes.SwapBlock;
            var original = particles.P_Move;
            particles.P_Move = customParticleColor;
            base.Update();
            particles.P_Move = original;
        }
    }
}
