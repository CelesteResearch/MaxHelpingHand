using System;
using System.Reflection;
using Celeste.Game.Execution.LevelLoader;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Modules
{
    public sealed partial class MaxHelpingHandModule
    {
        private void LoadRenderSettings()
        {
            hooks.Add(new Hook(
                typeof(OriginalLevelLoaderScene).GetMethod("StartLevel", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(typeof(OriginalLevelLoaderScene).FullName, "StartLevel"),
                (Action<Action<OriginalLevelLoaderScene>, OriginalLevelLoaderScene>)RestoreBloomStrength));
        }

        private void RestoreBloomStrength(Action<OriginalLevelLoaderScene> original, OriginalLevelLoaderScene loader)
        {
            if (ReferenceEquals(loader.EngineContext, SourceEngine) && Session.BloomStrength.HasValue)
                loader.Level.Bloom.Strength = Session.BloomStrength.Value;
            original(loader);
        }
    }
}
