using Celeste.Physics.Collision;

namespace Celeste.Mods.MaxHelpingHand.Collision
{
    public interface IWallSurfaceSoundCollider : ISourceEntityCollider
    {
        int WallSurfaceSoundIndex { get; }
    }
}
