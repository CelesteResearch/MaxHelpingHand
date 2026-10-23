using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Simulation.Presentation.Backdrops.HeatWave;
using Celeste.Simulation.Presentation.Backdrops.Runtime;

namespace Celeste.Mods.MaxHelpingHand.Backdrops
{
    public sealed class HeatWaveNoColorGrade : HeatWaveBackdropPresentation
    {
        private readonly ISourceEngineContext engine;
        private readonly bool controlColorGradeWhenActive;
        private readonly bool renderParticles;

        public HeatWaveNoColorGrade(ISourceEngineContext engine, bool controlColorGradeWhenActive,
            bool renderParticles) : base(engine)
        {
            this.engine = engine;
            this.controlColorGradeWhenActive = controlColorGradeWhenActive;
            this.renderParticles = renderParticles;
        }

        protected override int ParticleRenderCount => renderParticles ? base.ParticleRenderCount : 0;

        public override void Update(ISourceBackdropScene scene)
        {
            var level = (OriginalLevelSceneRoot)scene;
            bool show = IsVisible(scene) && scene.CoreMode != 0;
            if (!show || !controlColorGradeWhenActive)
            {
                if (show || FadeAmount > 0)
                {
                    string lastColorGrade = level.ColorGrade.LastGrade;
                    float colorGradeEase = level.ColorGrade.Ease;
                    float colorGradeEaseSpeed = level.ColorGrade.EaseSpeed;
                    string colorGrade = level.Session.ColorGrade;
                    base.Update(scene);
                    level.ColorGrade.LastGrade = lastColorGrade;
                    level.ColorGrade.Ease = colorGradeEase;
                    level.ColorGrade.EaseSpeed = colorGradeEaseSpeed;
                    level.Session.ColorGrade = colorGrade;
                    if (HeatAmount <= 0) engine.Effects.WaterSineDirection = 1f;
                }
            }
            else base.Update(scene);
        }
    }
}
