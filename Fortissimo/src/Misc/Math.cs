using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace FortissimoMath
{
    /// <summary>
    /// Gives a range for bytes.  Does interpolation on them also
    /// helpful for keeping Color ranges.
    /// </summary>
    public struct ByteRange
    {
        public ByteRange(byte Low, byte High) { this.Low = Low; this.High = High; }
        public byte InterpByte(double interp)
        {
            byte Dif = (byte)(High - Low);
            return (byte)(Low + (byte)(Dif * interp));
        }
        public byte Low;
        public byte High;
    }

    /// <summary>
    /// Helps with interpolation between two colors.  Can also be used
    /// to just store a simple range.
    /// </summary>
    public struct ColorRange
    {
        public ByteRange R;
        public ByteRange G;
        public ByteRange B;
        public ByteRange A;

        public double CurrentInterp;

        public ColorRange(Color From, Color To)
        {
            this.R = new ByteRange(From.R, To.R);
            this.G = new ByteRange(From.G, To.G);
            this.B = new ByteRange(From.B, To.B);
            this.A = new ByteRange(From.A, To.A);
            CurrentInterp = 0.0;
        }

        public ColorRange(ByteRange R, ByteRange G, ByteRange B, ByteRange A)
        {
            this.R = R;
            this.G = G;
            this.B = B;
            this.A = A;
            CurrentInterp = 0.0;
        }

        public ColorRange(byte Low, byte High)
        {
            this.R = new ByteRange(Low, High);
            this.G = new ByteRange(Low, High);
            this.B = new ByteRange(Low, High);
            this.A = new ByteRange(Low, High);
            CurrentInterp = 0.0;
        }

        public Color InterpColor()
        {
            return new Color(R.InterpByte(CurrentInterp), G.InterpByte(CurrentInterp), B.InterpByte(CurrentInterp), A.InterpByte(CurrentInterp));
        }

        public Color InterpColor(double value)
        {
            return new Color(R.InterpByte(value), G.InterpByte(value), B.InterpByte(value), A.InterpByte(value));
        }
    }
}
