using Celeste.Core.Reflection.Runtime;

namespace Celeste.Mods.MaxHelpingHand.Sessions.Music
{
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Module.MaxHelpingHandSession+SpeedBasedMusicParamInfo")]
    public class SpeedBasedMusicParamInfo
    {
        public float MinimumSpeed { get; set; }
        public float MaximumSpeed { get; set; }
        public float MinimumParamValue { get; set; }
        public float MaximumParamValue { get; set; }
    }
}
