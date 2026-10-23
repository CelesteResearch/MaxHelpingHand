using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.MaxHelpingHand.Runtime.Flags.Spawn;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.MaxHelpingHand.Triggers.Flags
{
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Triggers.SetFlagOnSpawnTrigger")]
    [CustomEntity("MaxHelpingHand/SetFlagOnSpawnTrigger")]
    public sealed class SetFlagOnSpawnTrigger : SourceTriggerEntityState
    {
        private readonly string[] flags;
        private readonly string ifFlag;
        private readonly bool enable;
        private readonly SpawnFlagHooks owner;

        public SetFlagOnSpawnTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
                data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            flags = data.Attr("flags", "").Split(',');
            ifFlag = data.Attr("ifFlag", "");
            enable = data.Bool("enable", false);
            owner = SpawnFlagHooks.For(engine);
            owner.Triggers.Add(this);
            Collider.Height += 1f;
        }

        internal void SetFlags(OriginalLevelSceneRoot level, Vector2 position)
        {
            if (!Collider.Collide(position) ||
                (!string.IsNullOrEmpty(ifFlag) && !level.Session.GetFlag(ifFlag))) return;
            foreach (string flag in flags)
                if (level != null)
                    level.Session.SetFlag(flag, enable);
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            RemoveSelf();
            owner.Triggers.Remove(this);
        }
    }
}
