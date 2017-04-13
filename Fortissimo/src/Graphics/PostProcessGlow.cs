using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace LightningSample
{
    class PostProcessGlow
    {
        GraphicsDevice graphicsDevice;
        int width;
        int height;
        Effect glowEffect;
        RenderTarget2D downsampleRT;
        RenderTarget2D temporaryRT;
        RenderTarget2D combineRT;
        SpriteBatch spriteBatch;

        public PostProcessGlow(GraphicsDevice graphicsDevice, int width, int height, ContentManager contentManager, SpriteBatch spriteBatch)
        {
            this.graphicsDevice = graphicsDevice;
            this.width = width;
            this.height = height;
            PresentationParameters pp = graphicsDevice.PresentationParameters;

            downsampleRT = new RenderTarget2D(graphicsDevice, width / 4, height / 4, true, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, pp.MultiSampleCount, RenderTargetUsage.DiscardContents);
            temporaryRT = new RenderTarget2D(graphicsDevice, width / 4, height / 4, true, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, pp.MultiSampleCount, RenderTargetUsage.DiscardContents);
            combineRT = new RenderTarget2D(graphicsDevice, width, height, true, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, pp.MultiSampleCount, RenderTargetUsage.DiscardContents);

            glowEffect = contentManager.Load<Effect>("LightningGlow");
            this.spriteBatch = spriteBatch;
        }

        private void DoTechnique(string technique, RenderTarget2D sourceRT, RenderTarget2D targetRT, int width, int height)
        {
            graphicsDevice.SetRenderTarget(targetRT); // 4.0change
            graphicsDevice.Clear(Color.Transparent);

            glowEffect.CurrentTechnique = glowEffect.Techniques[technique];
            glowEffect.Parameters["texelSize"].SetValue(new Vector2(1.0f / width, 1.0f / height));

            spriteBatch.Begin(0, BlendState.Opaque, null, null, null, glowEffect); // 4.0change
            //glowEffect.Begin();
            //glowEffect.CurrentTechnique.Passes[0].Begin();

            spriteBatch.Draw(sourceRT,  // 4.0change
                                new Rectangle(0,0,width,height),
                                Color.White);
            //glowEffect.CurrentTechnique.Passes[0].End();
            //glowEffect.End();
            spriteBatch.End();
            //resolve
            graphicsDevice.SetRenderTarget(null); // 4.0change

        }
        public void ApplyEffect(RenderTarget2D sourceRT, RenderTarget2D targetRT, float glowStrength)
        {
            //first downsample
            //downsample by rendering to smaller RT
            DoTechnique("Copy", sourceRT, downsampleRT, downsampleRT.Width, downsampleRT.Height);
            DoTechnique("BlurHorizontal", downsampleRT, temporaryRT, downsampleRT.Width, downsampleRT.Height);
            DoTechnique("BlurVertical", temporaryRT, downsampleRT, downsampleRT.Width, downsampleRT.Height);

            glowEffect.Parameters["glowMap"].SetValue(downsampleRT);
            glowEffect.Parameters["glowStrength"].SetValue(glowStrength);
            DoTechnique("Combine", sourceRT, combineRT, width, height);
            DoTechnique("Copy", combineRT, targetRT, width, height);
        }

        
    }
}
