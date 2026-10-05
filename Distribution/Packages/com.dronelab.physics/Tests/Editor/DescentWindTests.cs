using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DroneLab.Physics.Tests
{
    public sealed class DescentWindTests
    {
        private static string Read(string name)=>ProfileTestFiles.Read(name);
        private static ProfileLoadResult Load(Action<JObject> env=null,Action<JObject> drone=null,string profile="quad_test_descent_wind")
        {
            var e=JObject.Parse(Read("environment_dryden_frozen")); var d=JObject.Parse(Read(profile)); env?.Invoke(e); drone?.Invoke(d);
            return ProfileLoader.Load(d.ToString(),e.ToString(),Read("drone-profile.schema"),Read("environment-profile.schema"));
        }
        private static RuntimeDroneParameters Parameters(Action<JObject> env=null,Action<JObject> drone=null,string profile="quad_test_descent_wind")
        { var r=Load(env,drone,profile); Assert.That(r.Success,Is.True,string.Join("\n",r.Issues)); return r.Parameters; }
        // Independent Simpson quadrature, not the production CDF or its antiderivative.
        private static double Integral(int c,double lo,double hi,double phaseScale=0)
        {
            const int n=20000; double h=(hi-lo)/n,sum=0;
            for(int i=0;i<=n;i++)
            {
                double q=lo+i*h,d=1+q*q;
                double f=c==0 ? 2/(Math.PI*d) : (1+3*q*q)/(Math.PI*d*d);
                sum+=(i==0 || i==n ? 1 : i%2==0 ? 2 : 4)*f*Math.Cos(q*phaseScale);
            }
            return sum*h/3;
        }
        [TestCase(0)] [TestCase(1)] [TestCase(2)]
        public void OneSidedSpectrumAndBandVarianceMatchIndependentQuadrature(int component)
        {
            double expected=40/Math.PI;
            Assert.That(DrydenSpectrum.Density(component,.1,10,2),Is.EqualTo(expected).Within(1e-12));
            Assert.That(DrydenSpectrum.CumulativeFraction(component,1e10),Is.EqualTo(1).Within(1e-9));
            var p=Parameters(); var field=p.Environment.Dryden;
            double sigma=component==2 ? .25 : .4;
            Assert.That(field.Bands.Where(b=>b.Component==component).Sum(b=>b.Variance),
                Is.EqualTo(sigma*sigma*Integral(component,.02,20)).Within(1e-11));
            foreach(var b in field.Bands.Where(b=>b.Component==component))
                Assert.That(b.Amplitude*b.Amplitude/2,Is.EqualTo(b.Variance).Within(1e-14));
        }
        [TestCase(8)] [TestCase(32)] [TestCase(128)]
        public void ModeCountPreservesRetainedEnergyAndDoesNotRenormalizeMissingTails(int modes)
        {
            var f=Parameters(e=>e["dryden"]["modesPerComponent"]=modes).Environment.Dryden;
            Assert.That(f.Bands.Count,Is.EqualTo(3*modes));
            Assert.That(f.RetainedVariance.X,Is.EqualTo(.16*Integral(0,.02,20)).Within(1e-11));
            Assert.That(f.RetainedVariance.Y,Is.EqualTo(.16*Integral(1,.02,20)).Within(1e-11));
            Assert.That(f.RetainedVariance.Z,Is.LessThan(.0625));
        }
        [Test] public void ActualWindReconstructsAllBandsAndRespectsFiniteAmplitudeBounds()
        {
            var f=Parameters().Environment.Dryden;
            for(int k=0;k<200;k++)
            {
                var point=new DVector3(k*.13,5,-4); double t=k*.07,s=point.X-5*t; var components=new double[3];
                foreach(var b in f.Bands) components[b.Component]+=b.Amplitude*Math.Sin(b.WaveNumber*s+b.Phase);
                var expected=f.MeanWind+f.Direction*components[0]+f.Lateral*components[1]+f.Vertical*components[2];
                Assert.That((f.Sample(point,t)-expected).Length,Is.LessThan(1e-12));
                Assert.That(Math.Abs(components[0]),Is.LessThanOrEqualTo(f.ComponentAmplitudeBound.X));
                Assert.That(Math.Abs(components[1]),Is.LessThanOrEqualTo(f.ComponentAmplitudeBound.Y));
                Assert.That(Math.Abs(components[2]),Is.LessThanOrEqualTo(f.ComponentAmplitudeBound.Z));
            }
        }
        [Test] public void SeedEnsembleHasZeroMeanAndDeclaredFiniteBandVariance()
        {
            const int n=2048; var sums=new double[3]; var squares=new double[3]; var covariance=new double[3]; double cross=0;
            var reference=Parameters().Environment.Dryden;
            for(int seed=0;seed<n;seed++)
            {
                var f=Parameters(e=>e["turbulenceSeed"]=seed).Environment.Dryden;
                var v=f.Sample(new DVector3(2,7,3),.4)-f.MeanWind;
                var a=new[]{DVector3.Dot(v,f.Direction),DVector3.Dot(v,f.Lateral),v.Y};
                var later=f.Sample(new DVector3(2,7,3),2.4)-f.MeanWind;
                var b=new[]{DVector3.Dot(later,f.Direction),DVector3.Dot(later,f.Lateral),later.Y};
                for(int c=0;c<3;c++) { sums[c]+=a[c]; squares[c]+=a[c]*a[c]; covariance[c]+=a[c]*b[c]; } cross+=a[0]*a[1];
            }
            var target=new[]{reference.RetainedVariance.X,reference.RetainedVariance.Y,reference.RetainedVariance.Z};
            for(int c=0;c<3;c++)
            {
                double mean=sums[c]/n;
                Assert.That(Math.Abs(mean),Is.LessThan(.08*Math.Sqrt(target[c])));
                Assert.That(squares[c]/n-mean*mean,Is.EqualTo(target[c]).Within(.09*target[c]));
                double expectedCov=(c==2 ? .0625 : .16)*Integral(c,.02,20,c==2 ? 1 : .5);
                Assert.That(covariance[c]/n,Is.EqualTo(expectedCov).Within(.09*target[c]));
                TestContext.WriteLine($"Dryden component {c}: mean={mean:F5}, variance={squares[c]/n-mean*mean:F5} / target={target[c]:F5}, lag-2s covariance={covariance[c]/n:F5} / quadrature={expectedCov:F5}");
            }
            Assert.That(Math.Abs(cross/n),Is.LessThan(.08*Math.Sqrt(target[0]*target[1])));
        }
        [Test] public void FrozenAdvectionAndInfiniteTransverseCoherenceAreExplicit()
        {
            var f=Parameters().Environment.Dryden; var point=new DVector3(2,3,4); var a=f.Sample(point,2);
            Assert.That((f.Sample(point+f.Direction*15,5)-a).Length,Is.LessThan(1e-12));
            Assert.That((f.Sample(point+new DVector3(0,100,-200),2)-a).Length,Is.Zero);
            f.Sample(default,9); Assert.That((f.Sample(point,2)-a).Length,Is.Zero);
            Assert.That((Parameters(e=>e["turbulenceSeed"]=99).Environment.Sample(point,2)-a).Length,Is.GreaterThan(.01));
        }
        [TestCase(.005)] [TestCase(.01)] [TestCase(.02)]
        public void SamePhysicalTimeWindDoesNotDependOnQuerySchedule(double dt)
        {
            var f=Parameters().Environment; double time=0;
            for(int i=0;i<(int)(2/dt);i++) { f.Sample(new DVector3(i,0,0),time); time+=dt; }
            Assert.That((f.Sample(default,time)-f.Sample(default,2)).Length,Is.LessThan(1e-12));
        }
        [Test] public void SamplingDiagnosticIncludesRelativeAdvectionAndGustFrequency()
        {
            var e=Parameters().Environment;
            Assert.That(e.WindSamplingRatio(new DVector3(5,100,100),.02),Is.Zero);
            Assert.That(e.WindSamplingRatio(default,.02),Is.EqualTo(e.Dryden.MaxWaveNumber*5*.02/Math.PI).Within(1e-14));
            e=Parameters(j=> { j["gustEnabled"]=true; j["gustTimeScaleS"]=.01; }).Environment;
            Assert.That(e.WindSamplingRatio(new DVector3(5,0,0),.02),Is.EqualTo(4));
            Assert.That(Parameters(drone:d=>d["physicsConfiguration"]["modules"]["windInteraction"]=false).Environment.WindSamplingRatio(default,.02),Is.Null);
        }
        [Test] public void DisabledAndZeroSpectrumPreserveMeanAndImmutableSnapshot()
        {
            var result=Load(e=>e["dryden"]["sigmaUvwMps"]=new JArray(0,0,0));
            Assert.That(result.Success,Is.True); result.Environment.dryden.sigmaUvwMps[0]=30;
            Assert.That((result.Parameters.Environment.Sample(default,3)-new DVector3(5,0,0)).Length,Is.Zero);
            Assert.That(result.Parameters.Environment.Dryden.MaxWaveNumber,Is.Zero);
        }
        [TestCase("dryden")] [TestCase("turbulenceSeed")]
        public void RequiredDrydenConfigurationIsNeverSynthesized(string key)=>Assert.That(Load(e=>e.Remove(key)).Success,Is.False);
        [TestCase("sigmaUvwMps",-1)] [TestCase("lengthScaleUvwM",0)]
        public void InvalidVectorMagnitudeRejected(string key,double value)=>Assert.That(Load(e=>e["dryden"][key][0]=value).Success,Is.False);
        [TestCase(7)] [TestCase(129)] public void InvalidModeCountRejected(int modes)=>Assert.That(Load(e=>e["dryden"]["modesPerComponent"]=modes).Success,Is.False);
        [Test] public void NonHorizontalAxisAndReversedBandRejected()
        {
            Assert.That(Load(e=>e["dryden"]["advectionDirectionWorld"]=new JArray(0,1,0)).Success,Is.False);
            Assert.That(Load(e=>e["dryden"]["advectionDirectionWorld"]=new JArray(2,0,0)).Success,Is.False);
            Assert.That(Load(e=> { e["dryden"]["minDimensionlessWaveNumber"]=1; e["dryden"]["maxDimensionlessWaveNumber"]=1; }).Success,Is.False);
        }
        [TestCase(6,15,false)] [TestCase(-3,15,false)] [TestCase(6.01,0,true)] [TestCase(-3.01,0,true)] [TestCase(0,15.01,true)]
        public void RawFlowEnvelopeUsesDeclaredInclusiveLimitsWithoutClipping(double axial,double lateral,bool exceeded)
        {
            var r=Parameters().Rotors[0]; double rho=1.2,area=Math.PI*r.Diameter*r.Diameter/4;
            var s=RotorFlightEnvelope.Evaluate(r,500,2*rho*area,new DVector3(lateral,axial,0),new DVector3(0,1,0),rho);
            Assert.That(s.Exceeded,Is.EqualTo(exceeded)); Assert.That(s.AxialSpeed,Is.EqualTo(axial));
            Assert.That(s.LateralSpeed,Is.EqualTo(lateral)); Assert.That(s.DescentToHoverInflowRatio,Is.EqualTo(-axial).Within(1e-12));
        }
        [TestCase(0,2,RotorFlowRegime.Stopped)] [TestCase(500,0,RotorFlowRegime.NonPositiveThrust)]
        [TestCase(500,-2,RotorFlowRegime.NonPositiveThrust)] [TestCase(500,2,RotorFlowRegime.PositiveThrustDescent)]
        public void StoppedAndNonPositiveThrustDoNotInventHoverInflow(double omega,double thrust,RotorFlowRegime regime)
        {
            var s=RotorFlightEnvelope.Evaluate(Parameters().Rotors[0],omega,thrust,new DVector3(0,-4,0),new DVector3(0,1,0),1.2);
            Assert.That(s.Regime,Is.EqualTo(regime)); Assert.That(s.DescentToHoverInflowRatio.HasValue,Is.EqualTo(omega>0 && thrust>0));
        }
        [Test] public void EnvelopeIsReportOnlyAndUnknownWithoutAuthorLimits()
        {
            var a=Parameters().Rotors[0]; var b=Parameters(drone:d=> { foreach(var r in d["rotors"]) ((JObject)r).Remove("operatingEnvelope"); }).Rotors[0];
            var s=RotorFlightEnvelope.Evaluate(b,500,3,new DVector3(0,-20,0),new DVector3(0,1,0),1.2);
            Assert.That(s.Exceeded,Is.Null); Assert.That(a.Performance.Evaluate(500).Thrust,Is.EqualTo(b.Performance.Evaluate(500).Thrust));
        }
        [TestCase(.005)] [TestCase(.01)] [TestCase(.02)]
        public void PassiveDescentConvergesToIndependentQuadraticDragSolution(double dt)
        {
            var p=Parameters(env:e=>e["airDensityMode"]="Constant",profile:"quad_test_basic"); double c=.5*p.Density*p.DragCd.Y*p.DragArea.Y;
            double v=0;
            double Accel(double velocity)=>-p.Gravity+BodyAerodynamics.EvaluatePoint(p,new DVector3(0,velocity,0),p.Density).Force.Y/p.Mass;
            for(int i=0;i<(int)(5/dt);i++) v+=dt*Accel(v+dt*.5*Accel(v));
            double expected=-Math.Sqrt(p.Mass*p.Gravity/c)*Math.Tanh(Math.Sqrt(p.Gravity*c/p.Mass)*5);
            TestContext.WriteLine($"Passive descent dt={dt}: v={v:F7} m/s, analytic={expected:F7}");
            Assert.That(v,Is.EqualTo(expected).Within(.003));
            foreach(var r in p.Rotors) Assert.That(r.Performance.Evaluate(0,-200).Thrust,Is.Zero);
        }
        [TestCase(.005)] [TestCase(.01)] [TestCase(.02)]
        public void SpectralWindDragRemainsPassiveAndTranslationConverges(double dt)
        {
            var p=Parameters(env:e=>e["airDensityMode"]="Constant",profile:"quad_test_basic");
            DVector3 Run(double step)
            {
                DVector3 pos=default,v=default;
                for(int k=0;k<(int)(3/step);k++)
                {
                    var air=v-p.Environment.Sample(pos,k*step); var f=BodyAerodynamics.EvaluatePoint(p,air,p.Density).Force;
                    Assert.That(DVector3.Dot(air,f),Is.LessThanOrEqualTo(1e-12));
                    v+=f*(step/p.Mass); pos+=v*step;
                }
                return pos;
            }
            double error=(Run(dt)-Run(.001)).Length;
            TestContext.WriteLine($"Dryden wind drag dt={dt}: position error={error:F7} m against 1000 Hz");
            Assert.That(error,Is.LessThan(.035));
        }
    }
}
