using Celeste.Mods.MaxHelpingHand.Runtime.Spinners.Rainbow;

namespace Celeste.Mods.MaxHelpingHand.Modules
{
    public sealed partial class MaxHelpingHandModule
    {
        private RainbowColorProcess rainbowControllers;

        public RainbowColorProcess RainbowControllers =>
            rainbowControllers ??= new RainbowColorProcess(SourceEngine, this);

        private void LoadRainbowControllers() => RainbowControllers.Load();

        private void UnloadRainbowControllers()
        {
            rainbowControllers?.Dispose();
            rainbowControllers = null;
        }
    }
}
