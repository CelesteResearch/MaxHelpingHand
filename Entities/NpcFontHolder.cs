using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Game.Content.Fonts;
using Celeste.Scene.Entities;

namespace Celeste.Mods.MaxHelpingHand.Entities
{
    internal sealed class NpcFontHolder : SceneEntityState
    {
        private readonly OriginalFontRegistry fonts;
        private readonly string face;

        public NpcFontHolder(ISourceEngineContext engine, OriginalFontRegistry fonts, string face)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
            this.fonts = fonts;
            this.face = face;
        }

        public override void Removed(object scene)
        {
            base.Removed(scene);
            fonts.Unload(face);
        }

        public override void SceneEnd(object scene)
        {
            base.SceneEnd(scene);
            fonts.Unload(face);
        }
    }
}
