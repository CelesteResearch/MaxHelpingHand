using System.Numerics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Game.Gameplay.Hazards.Bumpers;
using Celeste.Mods.MaxHelpingHand.Runtime.Hazards.Bumpers;
using Celeste.Simulation.Gameplay.Environment.Core;
using Celeste.Simulation.Gameplay.Platforms.StaticMovers;

namespace Celeste.Mods.MaxHelpingHand.Entities.Hazards.Bumpers
{
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.BumperNotCoreMode")]
    public class BumperNotCoreMode : OriginalBumperState
    {
        private readonly bool notCoreMode;
        private readonly bool wobble;
        private Vector2 anchor;

        public BumperNotCoreMode(Vector2 position, Vector2? node, bool notCoreMode, bool wobble,
            ISourceEngineContext engine)
            : base(position, node, engine)
        {
            this.notCoreMode = notCoreMode;
            this.wobble = wobble;
            anchor = Position;
            if (notCoreMode) Remove(Get<CoreModeListenerState>());
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            if (notCoreMode)
            {
                BumperFields.SetFireMode(this, false);
                BumperFields.GetEvilSprite(this).Visible = false;
                BumperFields.GetSprite(this).Visible = true;
            }
        }

        protected SourceStaticMoverState MakeWobbleStaticMover() => !wobble
            ? new SourceStaticMoverState()
            : new SourceStaticMoverState { OnMove = move => anchor += move };

        public override void Update()
        {
            base.Update();
            if (wobble)
            {
                var sine = BumperFields.GetSine(this);
                Position = anchor + new Vector2(sine.Value * 3f, sine.ValueOverTwo * 2f);
            }
        }

        internal bool MatchesEngine(ISourceEngineContext value) => ReferenceEquals(engine, value);
    }
}
