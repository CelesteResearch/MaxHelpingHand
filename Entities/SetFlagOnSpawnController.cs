using System;
using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Mods.MaxHelpingHand.Modules;
using Celeste.Scene.Entities;

namespace Celeste.Mods.MaxHelpingHand.Entities
{
    [CustomEntity("MaxHelpingHand/SetFlagOnSpawnController")]
    public sealed class SetFlagOnSpawnController : SceneEntityState
    {
        public SetFlagOnSpawnController(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
            var module = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<MaxHelpingHandModule>().Single();
            var level = module.LoadingLevel ??
                        throw new InvalidOperationException("Spawn controller requires the active level-load scope.");
            if (!data.Bool("onlyOnRespawn", false) || module.IsRespawning)
            {
                string otherFlag = data.Attr("ifFlag");
                if (string.IsNullOrEmpty(otherFlag) || level.Session.GetFlag(otherFlag))
                {
                    foreach (string flag in data.Attr("flag").Split(','))
                        level.Session.SetFlag(flag, data.Bool("enable"));
                }
            }
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            RemoveSelf();
        }
    }
}
