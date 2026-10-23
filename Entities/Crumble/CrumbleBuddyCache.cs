namespace Celeste.Mods.MaxHelpingHand.Entities.Crumble
{
    internal sealed class CrumbleBuddyCache
    {
        public readonly CrumbleBuddyResult<CustomizableCrumblePlatform> getOnePlatformWithPlayerOnTop = new();
        public readonly CrumbleBuddyResult<CustomizableCrumblePlatform> getOnePlatformWithPlayerClimbing = new();
        public readonly CrumbleBuddyResult<bool> isGroupCollidingWithSomething = new();
    }
}
