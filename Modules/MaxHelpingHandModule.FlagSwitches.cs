// SPDX-License-Identifier: MIT

using System;
using System.Collections;
using System.Reflection;
using Celeste.Everest.Content.Maps.Processing;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Enemies.Seekers;
using Celeste.Game.Gameplay.Hazards.Puffer;
using Celeste.Mods.MaxHelpingHand.Content.Maps;
using Celeste.Mods.MaxHelpingHand.Entities.Switches;
using Celeste.Mods.MaxHelpingHand.Runtime.FlagSwitches;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Modules
{
    public sealed partial class MaxHelpingHandModule
    {
        internal FlagSwitchGroups FlagSwitchGroups { get; private set; }
        private FieldInfo seekerPushRadius;
        private FieldInfo seekerPhysicsHitbox;
        private FieldInfo pufferPushRadius;

        public override void PrepareMapDataProcessors(EverestMapDataFixup context)
        {
            context.Add(new FlagSwitchMapDataProcessor(FlagSwitchGroups));
            context.Add(
                new global::Celeste.Mods.MaxHelpingHand.Content.Maps.MultiRoomStrawberryMapDataProcessor(SourceEngine));
        }

        private void LoadFlagSwitches()
        {
            FlagSwitchGroups = new FlagSwitchGroups(SourceEngine);
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            seekerPushRadius = typeof(OriginalSeekerState).GetField("pushRadius", flags) ??
                               throw new MissingFieldException("Seeker.pushRadius");
            seekerPhysicsHitbox = typeof(OriginalSeekerState).GetField("physicsHitbox", flags) ??
                                  throw new MissingFieldException("Seeker.physicsHitbox");
            pufferPushRadius = typeof(OriginalPufferState).GetField("pushRadius", flags) ??
                               throw new MissingFieldException("Puffer.pushRadius");
            hooks.Add(new Hook(typeof(OriginalSeekerState).GetMethod("RegenerateCoroutine", flags),
                (Func<Func<OriginalSeekerState, IEnumerator>, OriginalSeekerState, IEnumerator>)SeekerRegenerate));
            hooks.Add(new Hook(typeof(OriginalPufferState).GetMethod("Explode", flags),
                (Action<Action<OriginalPufferState>, OriginalPufferState>)PufferExplode));
        }

        private IEnumerator SeekerRegenerate(Func<OriginalSeekerState, IEnumerator> original,
            OriginalSeekerState seeker)
        {
            IEnumerator routine = original(seeker);
            while (routine.MoveNext()) yield return routine.Current;
            if (seeker.Scene is not OriginalLevelSceneRoot level ||
                !ReferenceEquals(level.EngineContext, SourceEngine)) yield break;
            seeker.Collider = (ColliderGeometry)seekerPushRadius.GetValue(seeker);
            TurnOnColliding(seeker, level);
            seeker.Collider = (ColliderGeometry)seekerPhysicsHitbox.GetValue(seeker);
        }

        private void PufferExplode(Action<OriginalPufferState> original, OriginalPufferState puffer)
        {
            original(puffer);
            if (puffer.Scene is not OriginalLevelSceneRoot level ||
                !ReferenceEquals(level.EngineContext, SourceEngine)) return;
            var old = puffer.Collider;
            puffer.Collider = (ColliderGeometry)pufferPushRadius.GetValue(puffer);
            TurnOnColliding(puffer, level);
            puffer.Collider = old;
        }

        private static void TurnOnColliding(SceneEntityState source, OriginalLevelSceneRoot level)
        {
            foreach (var entity in level.SourceScene.Entities.GetTrackedEntities(typeof(FlagTouchSwitch)))
                if (SourceEntityCollision.Check(source, (FlagTouchSwitch)entity))
                    ((FlagTouchSwitch)entity).TurnOn();
        }
    }
}
