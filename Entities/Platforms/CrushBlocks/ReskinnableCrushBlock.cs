using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Gameplay.Platforms.CrushBlocks;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Graphics.Content;
using Celeste.Graphics.Display.Colors;
using Celeste.Mods.MaxHelpingHand.Runtime.CrushBlocks;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.MaxHelpingHand.Entities.Platforms.CrushBlocks
{
    [CustomEntity("MaxHelpingHand/ReskinnableCrushBlock = Create")]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.ReskinnableCrushBlock")]
    public class ReskinnableCrushBlock : EverestCrushBlockState
    {
        internal readonly ISourceEngineContext SourceEngine;
        private string spriteDirectory;
        private string soundDirectory;
        private ParticleTypePresentation crushParticleColor;
        internal ParticleTypePresentation activateParticleColor;

        public static ReskinnableCrushBlock Create(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(level.EngineContext, data, offset,
            level.SolidWorldFactory.Create(data.Position + offset, false));

        public ReskinnableCrushBlock(ISourceEngineContext engine, EntityData data, Vector2 offset,
            ISourceSolidWorld world)
            : base(engine, data, offset, world, data.Attr("spriteDirectory", "objects/crushblock"))
        {
            SourceEngine = engine;
            spriteDirectory = data.Attr("spriteDirectory", "objects/crushblock");
            soundDirectory = data.Attr("soundDirectory", "event:/game/06_reflection");
            var face = SourceFace;
            face.Reset(((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas, spriteDirectory + "/");
            if (SourceGiant)
            {
                face.AddLoop("idle", "giant_block", .08f, 0);
                face.Add("hurt", "giant_block", .08f, "idle", 8, 9, 10, 11, 12);
                face.Add("hit", "giant_block", .08f, 0, 1, 2, 3, 4, 5);
                face.AddLoop("right", "giant_block", .08f, 6, 7);
            }
            else
            {
                face.AddLoop("idle", "idle_face", .08f);
                face.Add("hurt", "hurt", .08f, "idle", 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);
                face.Add("hit", "hit", .08f);
                face.AddLoop("left", "hit_left", .08f);
                face.AddLoop("right", "hit_right", .08f);
                face.AddLoop("up", "hit_up", .08f);
                face.AddLoop("down", "hit_down", .08f);
            }

            face.CenterOrigin();
            face.Play("idle");
            SourceFill = SourceHexColor.HexToColor(data.Attr("fillColor", "62222b"));
            var particles = ((IOriginalGameplayProcess)engine).ParticleTypes.CrushBlock;
            crushParticleColor = new ParticleTypePresentation(particles.P_Crushing)
            {
                Color = SourceHexColor.HexToColor(data.Attr("crushParticleColor1", "ff66e2")),
                Color2 = SourceHexColor.HexToColor(data.Attr("crushParticleColor2", "68fcff"))
            };
            activateParticleColor = new ParticleTypePresentation(particles.P_Activate)
            {
                Color = SourceHexColor.HexToColor(data.Attr("activateParticleColor1", "5fcde4")),
                Color2 = SourceHexColor.HexToColor(data.Attr("activateParticleColor2", "ffffff"))
            };
        }

        protected override string ResolveSoundPath(string path)
            => ReskinnableCrushBlockHooks.IsLoaded(SourceEngine)
                ? path.Replace("event:/game/06_reflection", soundDirectory)
                : path;

        public override void Update()
        {
            var particles = ((IOriginalGameplayProcess)SourceEngine).ParticleTypes.CrushBlock;
            var original = particles.P_Crushing;
            particles.P_Crushing = crushParticleColor;
            base.Update();
            particles.P_Crushing = original;
        }
    }
}
