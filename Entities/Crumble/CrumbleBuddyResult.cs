namespace Celeste.Mods.MaxHelpingHand.Entities.Crumble
{
    internal sealed class CrumbleBuddyResult<T>
    {
        public T result;
        public bool computed;

        public void Reset()
        {
            result = default;
            computed = false;
        }
    }
}
