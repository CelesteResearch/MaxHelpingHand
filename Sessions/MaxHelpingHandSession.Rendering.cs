using YamlDotNet.Serialization;

namespace Celeste.Mods.MaxHelpingHand.Sessions
{
    public partial class MaxHelpingHandSession
    {
        [YamlMember(Alias = "BloomStregth")] public float? BloomStrength;
    }
}
