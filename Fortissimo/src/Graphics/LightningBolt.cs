using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
namespace LightningSample
{
    /// <summary>
    /// Describes the operation that will be applied to the lightning segments
    /// </summary>
    public enum LightningSubdivisionOp
    {
        /// <summary>
        /// Take a point on the line and modify its position
        /// </summary>
        Jitter,
        /// <summary>
        /// Take a point on the line, modify it's position, and 
        /// generate a new segment starting in this point
        /// </summary>
        JitterAndFork
    }


    public class LightningBolt
    {
        #region Members

        private Game game;
        private Vector3 source;
        private Vector3 destination;
        private RenderTarget2D lightningRT;
        private LightningVertex[] lightningPoints;
        private int[] indices;
        private VirtualLine[] virtualLines;
        private VirtualPoint[] virtualPoints;

        private int totalPointIndex;
        private int realVertexCount;
        private int totalRealVertices;
        private int totalIndices = 0;

        private Effect lightningDrawEffect;

        private Random rand = new Random();
        private SpriteBatch spriteBatch;
        private PostProcessGlow postProcessGlow;
        private LightningDescriptor properties;
        private List<LightningSubdivisionOp> topology;
        double millisecondsSinceLastAnimation=0;

        #endregion

        #region Properties

        /// <summary>
        /// Source of this lightning
        /// </summary>
        public Vector3 Source
        {
            get { return source; }
            set { source = value; }
        }

        /// <summary>
        /// Destination of this lightning
        /// </summary>
        public Vector3 Destination
        {
            get { return destination; }
            set { destination = value; }
        }

        /// <summary>
        /// The rendered lightning
        /// This texture should be drawn over the screen, in the end
        /// </summary>
        public Texture2D LightningTexture
        {
            get { return lightningRT; } // 4.0change
        }

        /// <summary>
        /// Descriptor for the lightning's behaviour
        /// </summary>
        public LightningDescriptor LightningDescriptor
        {
            get { return properties; }
            set { properties = value; }
        }

        private float ForkArmLength
        {
            get { return properties.ForkLengthPercentage * Vector3.Distance(source, destination); }
        } 

        #endregion

        #region Types

        /// <summary>
        /// Private structure that keeps track of lightning segments
        /// </summary>
        private struct VirtualLine
        {
            public int v0;
            public int v1;
            public int v2;
            public int v3;
            public int widthLevel;
        }

        /// <summary>
        /// Private structure that keeps track of lightning vertices
        /// </summary>
        private struct VirtualPoint
        {
            public int v0;
            public int v1;
            public int v2;
            public int v3;
            public int widthLevel;
        }

        
        #endregion

        #region Constructors

        public LightningBolt(Game game)
            : this(game, Vector3.Zero, Vector3.One, LightningDescriptor.Default)
        {
        }

        public LightningBolt(Game game, Vector3 source, Vector3 destination)
            : this(game, source, destination, LightningDescriptor.Default)
        {
        }
        public LightningBolt(Game game, Vector3 source, Vector3 destination, LightningDescriptor descriptor)
        {
            this.game = game;
            this.properties = descriptor;
            this.topology = properties.Topology;
            int lineCount;
            int pointCount;

            lineCount = ComputeNrLines(0);
            pointCount = ComputeNrPoints(0);
            virtualLines = new VirtualLine[lineCount];
            virtualPoints = new VirtualPoint[pointCount];



            indices = new int[lineCount * 6 + pointCount * 6];
            realVertexCount = lineCount * 4 + pointCount * 4;
            lightningPoints = new LightningVertex[realVertexCount];
            for (int i = 0; i < realVertexCount; i++)
            {
                lightningPoints[i] = new LightningVertex();
            }

            this.source = source;
            this.destination = destination;
            destination = new Vector3(30, 0, 0);
            totalRealVertices = 0;

            totalPointIndex = 0;
            AddPoint(1);
            AddPoint(1);
            BuildIndices(0, 0, 1);

            totalPointIndex = 0;
            SetPointPositions(source);
            SetPointPositions(destination);
            BuildVertices(0, source, destination, 0);


            lightningDrawEffect = game.Content.Load<Effect>("LightningDraw");

            PresentationParameters pp = game.GraphicsDevice.PresentationParameters;
            lightningRT = new RenderTarget2D(game.GraphicsDevice, pp.BackBufferWidth, pp.BackBufferHeight, true, SurfaceFormat.Color, DepthFormat.Depth24Stencil8, pp.MultiSampleCount, RenderTargetUsage.DiscardContents);
            spriteBatch = new SpriteBatch(game.GraphicsDevice);
            postProcessGlow = new PostProcessGlow(game.GraphicsDevice, pp.BackBufferWidth, pp.BackBufferHeight, game.Content, spriteBatch);
        } 
        #endregion

        #region Helper Functions
        private int ComputeNrLines(int level)
        {
            if (level == topology.Count - 1)
            {
                if (topology[level] == LightningSubdivisionOp.Jitter)
                    return 2;
                else
                    return 3;
            }

            if (topology[level] == LightningSubdivisionOp.Jitter)
                return 2 * ComputeNrLines(level + 1);
            else
                return 3 * ComputeNrLines(level + 1);
        }

        private int ComputeNrPoints(int level)
        {
            if (level == topology.Count - 1)
            {
                if (topology[level] == LightningSubdivisionOp.Jitter)
                    return 3;
                else
                    return 4;
            }

            if (topology[level] == LightningSubdivisionOp.Jitter)
                return 2 * ComputeNrPoints(level + 1) - 1;
            else
                return 3 * ComputeNrPoints(level + 1) - 2;
        }

        private float Decay(float amount, int level, float decayrate)
        {
            return amount * (float)Math.Pow(decayrate, level);
        }

        private float Random(Range range)
        {
            float nr = (float)rand.NextDouble();
            return MathHelper.Lerp(range.Min, range.Max, nr);
        }

        private Vector3 GetLeft(Vector3 forward)
        {
            return Vector3.Normalize(Vector3.Transform(forward, Matrix.CreateRotationZ(MathHelper.PiOver2)));
        }

        private Vector3 GetJittered(Vector3 start, Vector3 end, Vector3 forward, Vector3 left, int level)
        {
            Vector2 delta = Decay(properties.JitterDeviationRadius, level, properties.JitterDecayRate) *
                                new Vector2(Random(properties.JitterForwardDeviation), Random(properties.JitterLeftDeviation));

            return Vector3.Lerp(start, end, Random(properties.SubdivisionFraction)) + delta.X * forward + delta.Y * left;
        }
        private Vector3 GetForkDelta(Vector3 forward, Vector3 left, int level)
        {
            Vector2 fork_delta = Decay(ForkArmLength, level, properties.ForkDecayRate) *
                                new Vector2(Random(properties.ForkForwardDeviation), Random(properties.ForkLeftDeviation));
            return fork_delta.X * forward + fork_delta.Y * left;
        }

        private int BuildVertices(int level, Vector3 start, Vector3 end, int virtualLineIndex)
        {
            if (level == topology.Count)
            {
                SetLinePositions(virtualLineIndex, start, end);
                return virtualLineIndex + 1;
            }

            int lastLineIndex = virtualLineIndex;

            switch (topology[level])
            {
                case LightningSubdivisionOp.Jitter:
                    lastLineIndex = JitterStep(level, start, end, virtualLineIndex);
                    break;
                case LightningSubdivisionOp.JitterAndFork:
                    lastLineIndex = ForkStep(level, start, end, virtualLineIndex);
                    break;
            }
            return lastLineIndex;

        }

        private int JitterStep(int level, Vector3 start, Vector3 end, int virtualLineIndex)
        {
            Vector3 forward = Vector3.Normalize(end - start);
            Vector3 left = GetLeft(forward);

            Vector3 jittered = GetJittered(start, end, forward, left, level);

            int lastLineIndex;
            SetPointPositions(jittered);
            lastLineIndex = BuildVertices(level + 1, start, jittered, virtualLineIndex);
            lastLineIndex = BuildVertices(level + 1, jittered, end, lastLineIndex);
            return lastLineIndex;
        }


        private int ForkStep(int level, Vector3 start, Vector3 end, int virtualLineIndex)
        {
            Vector3 forward = Vector3.Normalize(end - start);
            Vector3 left = GetLeft(forward);

            Vector3 jittered = GetJittered(start, end, forward, left, level);
            Vector3 forked = jittered + GetForkDelta(forward, left, level);

            int lastLineIndex;
            SetPointPositions(jittered);
            SetPointPositions(forked);
            lastLineIndex = BuildVertices(level + 1, start, jittered, virtualLineIndex);
            lastLineIndex = BuildVertices(level + 1, jittered, forked, lastLineIndex);
            lastLineIndex = BuildVertices(level + 1, jittered, end, lastLineIndex);
            return lastLineIndex;
        }

        private float ComputeWidth(int widthLevel)
        {
            if (properties.IsWidthDecreasing)
                return properties.BaseWidth / widthLevel;
            else
                return properties.BaseWidth;
        }

        private void SetLinePositions(int virtualLineIndex, Vector3 start, Vector3 end)
        {
            int v0 = virtualLines[virtualLineIndex].v0;
            int v1 = virtualLines[virtualLineIndex].v1;
            int v2 = virtualLines[virtualLineIndex].v2;
            int v3 = virtualLines[virtualLineIndex].v3;

            Vector3 forward = Vector3.Normalize(end - start);
            Vector3 left = GetLeft(forward);
            float width = ComputeWidth(virtualLines[virtualLineIndex].widthLevel);

            lightningPoints[v0].Position = start + left * width;
            lightningPoints[v1].Position = end + left * width;
            lightningPoints[v2].Position = end - left * width;
            lightningPoints[v3].Position = start - left * width;
        }
        private void SetPointPositions(Vector3 position)
        {
            int pointIndex = totalPointIndex;
            int v0 = virtualPoints[pointIndex].v0;
            int v1 = virtualPoints[pointIndex].v1;
            int v2 = virtualPoints[pointIndex].v2;
            int v3 = virtualPoints[pointIndex].v3;

            float width = ComputeWidth(virtualPoints[pointIndex].widthLevel);

            lightningPoints[v0].Position = position + width * new Vector3(-1, -1, 0);
            lightningPoints[v1].Position = position + width * new Vector3(1, -1, 0);
            lightningPoints[v2].Position = position + width * new Vector3(1, 1, 0);
            lightningPoints[v3].Position = position + width * new Vector3(-1, 1, 0);
            totalPointIndex++;
        }


        private void AddPoint(int width)
        {
            int pointIndex = totalPointIndex;
            int v0, v1, v2, v3;
            v0 = totalRealVertices;
            v1 = totalRealVertices + 1;
            v2 = totalRealVertices + 2;
            v3 = totalRealVertices + 3;

            virtualPoints[pointIndex] = new VirtualPoint();

            virtualPoints[pointIndex].v0 = v0;
            virtualPoints[pointIndex].v1 = v1;
            virtualPoints[pointIndex].v2 = v2;
            virtualPoints[pointIndex].v3 = v3;
            virtualPoints[pointIndex].widthLevel = width;

            lightningPoints[v0].TextureCoordinates = new Vector2(0, 0);
            lightningPoints[v0].ColorGradient = new Vector2(-1, 1);
            lightningPoints[v1].TextureCoordinates = new Vector2(1, 0);
            lightningPoints[v1].ColorGradient = new Vector2(1, 1);
            lightningPoints[v2].TextureCoordinates = new Vector2(1, 1);
            lightningPoints[v2].ColorGradient = new Vector2(1, -1);
            lightningPoints[v3].TextureCoordinates = new Vector2(0, 1);
            lightningPoints[v3].ColorGradient = new Vector2(-1, -1);

            indices[totalIndices] = v0;
            indices[totalIndices + 1] = v1;
            indices[totalIndices + 2] = v2;

            indices[totalIndices + 3] = v0;
            indices[totalIndices + 4] = v2;
            indices[totalIndices + 5] = v3;

            totalRealVertices += 4;
            totalIndices += 6;
            totalPointIndex++;
        }

        private void AddLine(int virtualLineIndex, int width)
        {
            int v0, v1, v2, v3;
            v0 = totalRealVertices;
            v1 = totalRealVertices + 1;
            v2 = totalRealVertices + 2;
            v3 = totalRealVertices + 3;

            virtualLines[virtualLineIndex] = new VirtualLine();

            virtualLines[virtualLineIndex].v0 = v0;
            virtualLines[virtualLineIndex].v1 = v1;
            virtualLines[virtualLineIndex].v2 = v2;
            virtualLines[virtualLineIndex].v3 = v3;
            virtualLines[virtualLineIndex].widthLevel = width;

            lightningPoints[v0].TextureCoordinates = new Vector2(0, 0);
            lightningPoints[v0].ColorGradient = new Vector2(1, 0);
            lightningPoints[v1].TextureCoordinates = new Vector2(1, 0);
            lightningPoints[v1].ColorGradient = new Vector2(1, 0);
            lightningPoints[v2].TextureCoordinates = new Vector2(1, 1);
            lightningPoints[v2].ColorGradient = new Vector2(-1, 0);
            lightningPoints[v3].TextureCoordinates = new Vector2(0, 1);
            lightningPoints[v3].ColorGradient = new Vector2(-1, 0);

            indices[totalIndices] = v0;
            indices[totalIndices + 1] = v1;
            indices[totalIndices + 2] = v2;

            indices[totalIndices + 3] = v0;
            indices[totalIndices + 4] = v2;
            indices[totalIndices + 5] = v3;

            totalRealVertices += 4;
            totalIndices += 6;
        }

        private int BuildIndices(int level, int lineIndex, int width)
        {
            if (level == topology.Count)
            {
                AddLine(lineIndex, width);
                return lineIndex + 1;
            }
            int lastLineIndex = 0;

            switch (topology[level])
            {
                case LightningSubdivisionOp.Jitter:
                    AddPoint(width);
                    lastLineIndex = BuildIndices(level + 1, lineIndex, width);
                    lastLineIndex = BuildIndices(level + 1, lastLineIndex, width);
                    break;
                case LightningSubdivisionOp.JitterAndFork:
                    AddPoint(width);
                    AddPoint(width + 1);
                    lastLineIndex = BuildIndices(level + 1, lineIndex, width);
                    lastLineIndex = BuildIndices(level + 1, lastLineIndex, width + 1);
                    lastLineIndex = BuildIndices(level + 1, lastLineIndex, width);
                    break;
                default:
                    break;
            }
            return lastLineIndex;
        }


        
        #endregion

        /// <summary>
        /// Generates the lightning as drawn in the scene
        /// </summary>
        /// <param name="gameTime"></param>
        public void GenerateTexture(GameTime gameTime, Matrix world, Matrix view, Matrix projection)
        {
            GraphicsDevice device = game.GraphicsDevice;

            device.SetRenderTarget(lightningRT); // 4.0change
            device.Clear(ClearOptions.Target,Color.Transparent,1.0f,0); // 4.0change

            lightningDrawEffect.Parameters["World"].SetValue(world);
            lightningDrawEffect.Parameters["View"].SetValue(view);
            lightningDrawEffect.Parameters["Projection"].SetValue(projection);
            lightningDrawEffect.Parameters["StartColor"].SetValue(properties.InteriorColor.ToVector3());
            lightningDrawEffect.Parameters["EndColor"].SetValue(properties.ExteriorColor.ToVector3());

            device.BlendState = BlendState.AlphaBlend; // 4.0change
            // device.RenderState.BlendFunction = BlendFunction.Max; // 4.0change no idea, so I'm just commenting it out

            using (VertexDeclaration vdecl = new VertexDeclaration(LightningVertex.VertexElements)) // 4.0change
            {
                //device.VertexDeclaration = vdecl; // 4.0change
                //device.RasterizerState.CullMode = CullMode.None; // btftest
                //lightningDrawEffect.Begin();
                lightningDrawEffect.CurrentTechnique.Passes[0].Apply(); // 4.0change
                device.DrawUserIndexedPrimitives<LightningVertex>(PrimitiveType.TriangleList, lightningPoints, 0, lightningPoints.Length, indices, 0, indices.Length / 3); // 4.0change
                //lightningDrawEffect.CurrentTechnique.Passes[0].End(); // 4.0change
                //lightningDrawEffect.End();
            }
            device.SetRenderTarget(null);
            if(properties.IsGlowEnabled)
                postProcessGlow.ApplyEffect(lightningRT, lightningRT, properties.GlowIntensity);

            device.BlendState = BlendState.Additive; // 4.0change
        }

        /// <summary>
        /// Update Lightning Animation
        /// </summary>
        public void Update(GameTime gameTime)
        {

            if (properties.AnimationFramerate == 0.0f)
                return;

            if (properties.AnimationFramerate == -1.0f)
            {
                totalPointIndex = 0;
                SetPointPositions(source);
                SetPointPositions(destination);
                BuildVertices(0, source, destination, 0);
                return;
            }

            millisecondsSinceLastAnimation += gameTime.ElapsedGameTime.TotalMilliseconds;
            float frameLength = 1000.0f / properties.AnimationFramerate;
            if (millisecondsSinceLastAnimation > frameLength)
            {
                millisecondsSinceLastAnimation -= frameLength;
                totalPointIndex = 0;
                SetPointPositions(source);
                SetPointPositions(destination);
                BuildVertices(0, source, destination, 0);
            }
        }
    }
}
