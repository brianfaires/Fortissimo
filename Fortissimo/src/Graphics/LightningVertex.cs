using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace LightningSample
{
    struct LightningVertex : IVertexType
    {
        public Vector3 Position;
        public Vector2 TextureCoordinates;
        public Vector2 ColorGradient;
        
        // Describe the layout of this vertex structure.
        public static readonly VertexElement[] VertexElements =
        {   // 4.0change
            new VertexElement(0, VertexElementFormat.Vector3,
                                    VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Vector2,
                                     VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(20, VertexElementFormat.Vector2,
                                     VertexElementUsage.TextureCoordinate, 1),
        };
            // Describe the size of this vertex structure.
            public const int SizeInBytes = 28;

            VertexDeclaration IVertexType.VertexDeclaration
            {
                get { return new VertexDeclaration(VertexElements); }
            }
    }
}
