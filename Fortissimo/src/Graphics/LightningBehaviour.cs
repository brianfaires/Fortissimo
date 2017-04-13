using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;

namespace LightningSample
{
    public struct Range
    {
        public Range(float min, float max)
        {
            Min = min;
            Max = max;
        }
        public float Min;
        public float Max;
    }

    public class LightningDescriptor
    {
        private Range subdivisionFraction = new Range(0.45f, 0.55f);
        private Range jitterForwardDeviation= new Range(-1.0f, 1.0f);
        private Range jitterLeftDeviation = new Range(-1.0f, 1.0f);
        private float jitterDeviationRadius = 3.0f;
        private float jitterDecayRate = 0.6f;
        private float forkLengthPercentage = 0.5f;
        private float forkDecayRate = 0.5f;
        private Range forkForwardDeviation = new Range(0.0f, 1.0f);
        private Range forkLeftDeviation = new Range(-1.0f, 1.0f);
        private Color interiorColor = Color.White;
        private Color exteriorColor = Color.Blue;
        private float baseWidth = 0.6f;
        private bool isWidthDecreasing = true;
        private bool isGlowEnabled = true;
        private float glowIntensity = 0.5f;
        private float animationFramerate = -1.0f;
        private List<LightningSubdivisionOp> topology = new List<LightningSubdivisionOp>();

        public static LightningDescriptor Default
        {
            get {
                LightningDescriptor ld = new LightningDescriptor();
                ld.topology.Add(LightningSubdivisionOp.JitterAndFork);
                ld.topology.Add(LightningSubdivisionOp.JitterAndFork);
                ld.topology.Add(LightningSubdivisionOp.Jitter);
                ld.topology.Add(LightningSubdivisionOp.Jitter);
                ld.topology.Add(LightningSubdivisionOp.JitterAndFork);
                return ld; }
        }

        public static LightningDescriptor ElectricityBolt
        {
            get {
                LightningDescriptor ld = new LightningDescriptor();
                ld.ForkDecayRate = 1.0f;
                ld.ForkLengthPercentage = 0.2f;
                ld.BaseWidth = 0.3f;
                ld.ExteriorColor = Color.Navy;
                ld.ForkForwardDeviation = new Range(-1, 1);
                ld.ForkLeftDeviation = new Range(-1, 1);
                ld.ForkLengthPercentage = 0.2f;
                ld.GlowIntensity = 1.5f;
                ld.InteriorColor = Color.White;
                ld.IsGlowEnabled = true;
                ld.IsWidthDecreasing = false;
                ld.JitterDecayRate = 0.5f;
                ld.SubdivisionFraction = new Range(0.45f, 0.55f);
                ld.Topology.Add(LightningSubdivisionOp.Jitter);
                ld.Topology.Add(LightningSubdivisionOp.Jitter);
                ld.Topology.Add(LightningSubdivisionOp.Jitter);
                ld.Topology.Add(LightningSubdivisionOp.Jitter);
                ld.Topology.Add(LightningSubdivisionOp.Jitter);
                ld.Topology.Add(LightningSubdivisionOp.Jitter);
                return ld;
            }
        }

        public static LightningDescriptor ExamplePreset
        {
            get
            {
                LightningDescriptor ld = new LightningDescriptor();
                ld.BaseWidth = 0.3f;
                ld.ExteriorColor = Color.Red;
                ld.ForkDecayRate = 0.7f;
                ld.ForkForwardDeviation = new Range(-1, 1);
                ld.ForkLeftDeviation = new Range(-1, 1);
                ld.ForkLengthPercentage = 0.2f;
                ld.GlowIntensity = 2.5f;
                ld.InteriorColor = Color.Green;
                ld.IsGlowEnabled = true;
                ld.IsWidthDecreasing = false;
                ld.JitterDeviationRadius = 0.4f;
                ld.JitterDecayRate = 1.0f;
                ld.SubdivisionFraction = new Range(0.2f, 0.8f);
                ld.AnimationFramerate = 20.0f;
                ld.Topology.Add(LightningSubdivisionOp.Jitter);
                ld.Topology.Add(LightningSubdivisionOp.Jitter);
                ld.Topology.Add(LightningSubdivisionOp.Jitter);
                ld.Topology.Add(LightningSubdivisionOp.Jitter);
                ld.Topology.Add(LightningSubdivisionOp.JitterAndFork);
                ld.Topology.Add(LightningSubdivisionOp.Jitter);
                ld.Topology.Add(LightningSubdivisionOp.Jitter);

                return ld;
            }
        }



        public List<LightningSubdivisionOp> Topology
        {
            get { return topology; }
            set { topology = value; }
        }

        public float AnimationFramerate
        {
            get { return animationFramerate; }
            set { animationFramerate = value; }
        }

        public float GlowIntensity
        {
            get { return glowIntensity; }
            set { glowIntensity = value; }
        }

        public bool IsGlowEnabled
        {
            get { return isGlowEnabled; }
            set { isGlowEnabled = value; }
        }

        public bool IsWidthDecreasing
        {
            get { return isWidthDecreasing; }
            set { isWidthDecreasing = value; }
        }
        public float BaseWidth
        {
            get { return baseWidth; }
            set { baseWidth = value; }
        }

        public Color ExteriorColor
        {
            get { return exteriorColor; }
            set { exteriorColor = value; }
        }

        public Color InteriorColor
        {
            get { return interiorColor; }
            set { interiorColor = value; }
        }
	
        public Range SubdivisionFraction
        {
            get { return subdivisionFraction; }
            set { subdivisionFraction = value; }
        }

        public Range JitterForwardDeviation
        {
            get { return jitterForwardDeviation; }
            set { jitterForwardDeviation = value; }
        }


    	public Range JitterLeftDeviation
	    {
		    get { return jitterLeftDeviation;}
		    set { jitterLeftDeviation = value;}
	    }

        public float JitterDeviationRadius
        {
            get { return jitterDeviationRadius; }
            set { jitterDeviationRadius = value; }
        }

        public float JitterDecayRate
        {
            get { return jitterDecayRate; }
            set { jitterDecayRate = value; }
        }

        public float ForkLengthPercentage
        {
            get { return forkLengthPercentage; }
            set { forkLengthPercentage = value; }
        }

        public float ForkDecayRate
        {
            get { return forkDecayRate; }
            set { forkDecayRate = value; }
        }

        public Range ForkForwardDeviation
        {
            get { return forkForwardDeviation; }
            set { forkForwardDeviation = value; }
        }

        public Range ForkLeftDeviation
        {
            get { return forkLeftDeviation; }
            set { forkLeftDeviation = value; }
        }
	
	}
}
