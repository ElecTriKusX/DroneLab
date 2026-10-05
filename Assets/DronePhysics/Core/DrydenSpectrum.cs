using System;
using System.Collections.Generic;

namespace DroneLab.Physics
{
    public readonly struct DrydenBand
    {
        public readonly int Component;
        public readonly double WaveNumber,LowerWaveNumber,UpperWaveNumber,Variance,Amplitude,Phase;
        internal DrydenBand(int component,double lower,double upper,double variance,double phase)
        { Component=component; LowerWaveNumber=lower; UpperWaveNumber=upper; WaveNumber=Math.Sqrt(lower*upper);
          Variance=variance; Amplitude=Math.Sqrt(2*variance); Phase=phase; }
    }
    public static class DrydenSpectrum
    {
        // ONE-SIDED spatial PSD: integral over k>=0 equals sigma^2. k in rad/m.
        public static double Density(int component,double waveNumber,double length,double sigma)
        {
            Validate(component,waveNumber,length,sigma); double q=length*waveNumber,q2=q*q;
            return sigma*sigma*length/Math.PI*(component==0 ? 2/(1+q2) : (1+3*q2)/((1+q2)*(1+q2)));
        }
        public static double CumulativeFraction(int component,double dimensionlessWaveNumber)
        {
            Validate(component,dimensionlessWaveNumber,1,1); double q=dimensionlessWaveNumber;
            return (2*Math.Atan(q)-(component==0 ? 0 : q/(1+q*q)))/Math.PI;
        }
        private static void Validate(int component,double waveNumber,double length,double sigma)
        {
            EnvironmentMath.Finite(waveNumber); EnvironmentMath.Finite(length); EnvironmentMath.Finite(sigma);
            if(component<0 || component>2 || waveNumber<0 || waveNumber>1e10 || length<=0 || length>1e6 || sigma<0 || sigma>30)
                throw new ArgumentOutOfRangeException(nameof(component));
        }
    }
    // Finite random-phase spectral synthesis on ONE frozen spatial line. Not a full 3D field,
    // not a recursive Dryden white-noise filter, and not MIL gust-gradient/angular forcing.
    public sealed class DrydenFrozenField : IWindProvider
    {
        public readonly DVector3 Direction,Lateral,Vertical,MeanWind;
        public readonly double AdvectionSpeed,MaxWaveNumber;
        public readonly IReadOnlyList<DrydenBand> Bands;
        public readonly DVector3 RetainedVariance,ComponentAmplitudeBound;
        internal DrydenFrozenField(DrydenProfile p,int seed,DVector3 mean)
        {
            Direction=DVector3.From(p.advectionDirectionWorld).Normalized; Vertical=new DVector3(0,1,0);
            Lateral=DVector3.Cross(Vertical,Direction).Normalized; MeanWind=mean; AdvectionSpeed=p.advectionSpeedMps;
            var bands=new DrydenBand[3*p.modesPerComponent]; var variances=new double[3]; var bounds=new double[3];
            double ratio=Math.Pow(p.maxDimensionlessWaveNumber/p.minDimensionlessWaveNumber,1.0/p.modesPerComponent);
            for(int c=0;c<3;c++) for(int i=0;i<p.modesPerComponent;i++)
            {
                double lo=p.minDimensionlessWaveNumber*Math.Pow(ratio,i);
                double hi=i==p.modesPerComponent-1 ? p.maxDimensionlessWaveNumber : p.minDimensionlessWaveNumber*Math.Pow(ratio,i+1);
                double variance=p.sigmaUvwMps[c]*p.sigmaUvwMps[c]*(DrydenSpectrum.CumulativeFraction(c,hi)-DrydenSpectrum.CumulativeFraction(c,lo));
                int id=c*p.modesPerComponent+i;
                double phase=2*Math.PI*Uniform(unchecked((uint)seed+(uint)(id+1)*0x9e3779b9u));
                var b=new DrydenBand(c,lo/p.lengthScaleUvwM[c],hi/p.lengthScaleUvwM[c],Math.Max(0,variance),phase);
                bands[id]=b; variances[c]+=b.Variance; bounds[c]+=b.Amplitude;
                if(b.Amplitude>0) MaxWaveNumber=Math.Max(MaxWaveNumber,b.WaveNumber);
            }
            Bands=Array.AsReadOnly(bands); RetainedVariance=new DVector3(variances[0],variances[1],variances[2]);
            ComponentAmplitudeBound=new DVector3(bounds[0],bounds[1],bounds[2]);
        }
        private static double Uniform(uint x)
        { unchecked { x^=x>>16; x*=0x85ebca6bu; x^=x>>13; x*=0xc2b2ae35u; x^=x>>16; } return (x+.5)/4294967296.0; }
        public DVector3 Sample(DVector3 position,double time)
        {
            EnvironmentMath.Finite(position); EnvironmentMath.Finite(time);
            if(time<0) throw new ArgumentOutOfRangeException(nameof(time));
            double s=DVector3.Dot(position,Direction)-AdvectionSpeed*time,u=0,v=0,w=0;
            for(int i=0;i<Bands.Count;i++)
            {
                var b=Bands[i]; double value=b.Amplitude*Math.Sin(b.WaveNumber*s+b.Phase);
                if(b.Component==0) u+=value; else if(b.Component==1) v+=value; else w+=value;
            }
            var result=MeanWind+Direction*u+Lateral*v+Vertical*w; EnvironmentMath.Finite(result); return result;
        }
        // Highest active frequency divided by Nyquist. >0.5 has <4 samples/cycle, >1 aliases.
        public double SamplingRatio(DVector3 pointVelocity,double dt)
        {
            EnvironmentMath.Finite(pointVelocity); EnvironmentMath.Finite(dt);
            if(dt<=0) throw new ArgumentOutOfRangeException(nameof(dt));
            return MaxWaveNumber*Math.Abs(DVector3.Dot(pointVelocity,Direction)-AdvectionSpeed)*dt/Math.PI;
        }
    }
}
