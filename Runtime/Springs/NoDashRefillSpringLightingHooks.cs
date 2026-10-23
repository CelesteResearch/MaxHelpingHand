using System;
using Celeste.Core.Display;
using Celeste.Core.Rendering.States;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Graphics;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Presentation.Lighting;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Rendering.Devices;
using Celeste.Graphics.Rendering.Drawing;
using Celeste.Mods.MaxHelpingHand.Entities.Springs;
using Celeste.Scene.Tracking;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.MaxHelpingHand.Runtime.Springs
{
    public sealed class NoDashRefillSpringLightingHooks : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly Hook beforeRender;

        public NoDashRefillSpringLightingHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            beforeRender = new Hook(
                typeof(LightingRendererPresentation).GetMethod(nameof(LightingRendererPresentation.BeforeRender)),
                (Action<Action<LightingRendererPresentation, object>, LightingRendererPresentation, object>)
                BeforeRender);
        }

        private void BeforeRender(Action<LightingRendererPresentation, object> original,
            LightingRendererPresentation self, object scene)
        {
            original(self, scene);
            if (scene is not OriginalLevelSceneRoot level || !ReferenceEquals(level.EngineContext, engine)) return;
            var springs = ((ISourceScene)scene).SourceScene.Entities.GetTrackedEntities(typeof(NoDashRefillSpring));
            if (springs.Count == 0) return;
            var resources = (OriginalGameGraphicsResources)((ISourceGameGraphicsEnvironment)engine).GameGraphics;
            var scope = (ISourceDrawScope)engine;
            ((ISourceGraphicsDeviceEnvironment)engine).GraphicsDevice.SetRenderTarget(resources.Buffers.Light.Target);
            scope.Draw.SpriteBatch.Begin(SourceSpriteSortMode.Deferred, SourceBlendState.AlphaBlend,
                SourceSamplerState.PointClamp, SourceDepthStencilState.None, SourceRasterizerState.CullNone);
            foreach (var entity in springs)
                if (entity is NoDashRefillSpring spring && spring.ignoreLighting)
                {
                    var sprite = spring.Get<Sprite>();
                    string path = sprite.Texture.Name;
                    path = path.Substring(0, path.LastIndexOf('/')) + "/white_" +
                           path.Substring(path.LastIndexOf('/') + 1);
                    SourceAtlasDrawing.Draw(scope, resources.GameAtlas[path],
                        spring.Position + sprite.Position - level.Camera.Position, sprite.Origin,
                        PackedColor.White, sprite.Scale, sprite.Rotation);
                }

            scope.Draw.SpriteBatch.End();
        }

        public void Dispose() => beforeRender.Dispose();
    }
}
