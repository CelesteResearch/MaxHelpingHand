using System.Collections.Generic;
using Celeste.Formats.Map.Entities.Structure;

namespace Celeste.Mods.MaxHelpingHand.Sessions
{
    public sealed class SeekerBarrierColorState
    {
        public string Color { get; set; }
        public string ParticleColor { get; set; }
        public float Transparency { get; set; }
        public float ParticleTransparency { get; set; }
        public float ParticleDirection { get; set; }
        public int? Depth { get; set; }
        public bool Wavy { get; set; }
        public bool RenderBloom { get; set; }

        internal EntityData Restore() => new()
        {
            Values = new Dictionary<string, object>
            {
                ["color"] = Color, ["particleColor"] = ParticleColor, ["transparency"] = Transparency,
                ["particleTransparency"] = ParticleTransparency, ["particleDirection"] = ParticleDirection,
                ["depth"] = Depth?.ToString() ?? "", ["wavy"] = Wavy, ["renderBloom"] = RenderBloom,
                ["persistent"] = true
            }
        };
    }
}
