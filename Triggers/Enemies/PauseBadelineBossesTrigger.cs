using System.Numerics;
using System.Collections.Generic;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Enemies.BadelineBoss;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.MaxHelpingHand.Triggers.Enemies
{
    [CustomEntity("MaxHelpingHand/PauseBadelineBossesTrigger")]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Triggers.PauseBadelineBossesTrigger")]
    public class PauseBadelineBossesTrigger : SourceTriggerEntityState
    {
        private readonly string bossKey;

        public PauseBadelineBossesTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(
            ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
            data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            bossKey = ((ISourceSceneCatalogEnvironment)engine).SceneCatalog.Find(typeof(FinalBossState)).RegistryKey;
        }

        public override void OnStay(RoomPlayerSession player)
        {
            base.OnStay(player);
            foreach (FinalBossState boss in ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(bossKey))
            {
                if (boss.Sprite?.CurrentAnimationID == "idle") boss.Remove(boss.AttackCoroutine);
            }
        }

        public override void OnLeave(RoomPlayerSession player)
        {
            base.OnLeave(player);
            if (player.Dead) return;
            foreach (FinalBossState boss in ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(bossKey))
            {
                var attack = boss.AttackCoroutine;
                bool contains = false;
                for (int i = 0; i < boss.OwnedComponents.Count; i++)
                {
                    if (EqualityComparer<SourceComponentState>.Default.Equals(boss.OwnedComponents[i], attack))
                    {
                        contains = true;
                        break;
                    }
                }

                if (!contains) boss.Add(attack);
            }
        }

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new PauseBadelineBossesTrigger(level.EngineContext, data, offset);
    }
}
