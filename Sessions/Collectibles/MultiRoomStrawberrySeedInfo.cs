using System.Numerics;
using Celeste.Core.Maps;
using Celeste.Core.Reflection.Runtime;

namespace Celeste.Mods.MaxHelpingHand.Sessions.Collectibles
{
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Module.MaxHelpingHandSession+MultiRoomStrawberrySeedInfo")]
    public class MultiRoomStrawberrySeedInfo
    {
        public int Index { get; set; }
        public MapEntityId BerryID { get; set; }
        public string Sprite { get; set; }
        public string StartingRoom { get; set; }
        public Vector2 StartingPoint { get; set; }
    }
}
