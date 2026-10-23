// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;

namespace Celeste.Mods.MaxHelpingHand.Entities.Glass
{
    [CustomEntity("MaxHelpingHand/CustomizableGlassBlockAreaController")]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.CustomizableGlassBlockAreaController")]
    public sealed class CustomizableGlassBlockAreaController : CustomizableGlassBlockController
    {
        public CustomizableGlassBlockAreaController(ISourceEngineContext engine, EntityData data, Vector2 offset) :
            base(
                engine, data, offset)
        {
            Collider = new HitboxColliderGeometry(data.Width, data.Height);
        }

        protected override IReadOnlyList<object> GetGlassBlocks()
        {
            var blocks = new List<object>();
            foreach (CustomizableGlassBlock block in base.GetGlassBlocks())
                if (SourceEntityCollision.Check(this, block))
                    blocks.Add(block);
            return blocks;
        }
    }
}
