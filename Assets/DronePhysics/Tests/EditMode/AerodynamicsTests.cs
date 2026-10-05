using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DroneLab.Physics.Tests
{
    public sealed class AerodynamicsTests
    {
        private static string Read(string file)
        {
#if UNITY_EDITOR
            string folder=Path.Combine(UnityEngine.Application.dataPath,"DronePhysics/Resources/DronePhysics");
#else
            string folder=Path.Combine(TestContext.CurrentContext.TestDirectory,"Profiles");
#endif
            return File.ReadAllText(Path.Combine(folder,file+".json"));
        }
        private static ProfileLoadResult Load(string name,Action<JObject> edit=null)
        {
            var json=JObject.Parse(Read(name)); edit?.Invoke(json);
            return ProfileLoader.Load(json.ToString(),Read("environment_calm"),Read("drone-profile.schema"),Read("environment-profile.schema"));
        }
        [TestCase("projected_box")] [TestCase("projected_lut")] [TestCase("surfaces")]
        public void StageThreeFixturesLoad(string mode)
        {
            var r=Load("quad_test_"+mode); Assert.That(r.Success,Is.True,string.Join("\n",r.Issues));
        }
        [TestCase(1,0,0,0.04)] [TestCase(0,1,0,0.16)] [TestCase(0,0,1,0.04)]
        public void BoxAxisAreasMatchAnalyticGeometry(double x,double y,double z,double expected)
            => Assert.That(BodyAerodynamics.BoxProjectedArea(new DVector3(0.4,0.1,0.4),new DVector3(x,y,z)),Is.EqualTo(expected).Within(1e-12));
        [Test] public void ProjectedDragOpposesAirVelocityAndScalesWithSpeedSquaredAndDensity()
        {
            var v=new DVector3(2,-3,4); var f=BodyAerodynamics.ProjectedDrag(v,1.225,1,0.2);
            Assert.That(DVector3.Dot(f,v),Is.LessThan(0)); Assert.That(DVector3.Cross(f,v).Length,Is.LessThan(1e-12));
            Assert.That((BodyAerodynamics.ProjectedDrag(v*2,1.225,1,0.2)-f*4).Length,Is.LessThan(1e-12));
            Assert.That((BodyAerodynamics.ProjectedDrag(v,2.45,1,0.2)-f*2).Length,Is.LessThan(1e-12));
            Assert.That(BodyAerodynamics.ProjectedDrag(default,1.225,1,0.2).Length,Is.Zero);
        }
        [Test] public void SurfaceIsTwoSidedWithNoTangentialForce()
        {
            var n=new DVector3(0,1,0); var v=new DVector3(2,3,4);
            var f=BodyAerodynamics.SurfaceDrag(v,n,1,1,1);
            Assert.That(f.Y,Is.EqualTo(-4.5)); Assert.That(f.X,Is.Zero); Assert.That(f.Z,Is.Zero);
            Assert.That((BodyAerodynamics.SurfaceDrag(v,n*-1,1,1,1)-f).Length,Is.Zero);
            Assert.That((BodyAerodynamics.SurfaceDrag(v*-1,n,1,1,1)+f).Length,Is.Zero);
            Assert.That(BodyAerodynamics.SurfaceDrag(new DVector3(1,0,1),n,1,1,1).Length,Is.Zero);
        }
        [Test] public void SurfaceVelocityIncludesRotationAndBrakesYawWithoutTranslation()
        {
            var p=Load("quad_test_surfaces").Parameters; var omega=new DVector3(0,2,0);
            var wrench=BodyAerodynamics.Evaluate(p,default,omega);
            Assert.That(wrench.Force.Length,Is.LessThan(1e-12)); Assert.That(wrench.Torque.Y,Is.LessThan(0));
            Assert.That(DVector3.Dot(wrench.Torque,omega),Is.LessThan(0));
        }
        [Test] public void ForceAndTorqueTogetherDissipateEnergyForArbitraryMotion()
        {
            var p=Load("quad_test_surfaces").Parameters;
            var v=new DVector3(2,3,-4); var omega=new DVector3(5,-2,1);
            var w=BodyAerodynamics.Evaluate(p,v,omega);
            Assert.That(DVector3.Dot(w.Force,v)+DVector3.Dot(w.Torque,omega),Is.LessThan(0));
        }
        [TestCase("quad_test_basic")] [TestCase("quad_test_projected_box")]
        public void CpOffsetUsesPointVelocityAndProducesCorrectMoment(string name)
        {
            var p=Load(name,j=>j["bodyAerodynamics"]["dragApplicationPointLocalM"]=new JArray(0.2,0,0)).Parameters;
            var omega=new DVector3(0,2,0); var r=p.DragPoint-p.CenterOfMass;
            var w=BodyAerodynamics.Evaluate(p,default,omega);
            Assert.That(w.Force.Z,Is.GreaterThan(0)); Assert.That(w.Torque.Y,Is.LessThan(0));
            Assert.That((w.Torque-DVector3.Cross(r,w.Force)).Length,Is.LessThan(1e-12));
        }
        [Test] public void AxisModelRetainsLegacyForceExactly()
        {
            var p=Load("quad_test_basic").Parameters; var v=new DVector3(3,-2,4);
            var w=BodyAerodynamics.Evaluate(p,v,default);
            Assert.That((w.Force-PhysicsMath.AxisDrag(v,p.Density,p.DragCd,p.DragArea)).Length,Is.Zero);
        }
        [Test] public void DisabledBodyDragDoesNotApplyAnyModel()
        {
            var p=Load("quad_test_surfaces",j=>j["physicsConfiguration"]["modules"]["bodyDrag"]=false).Parameters;
            var w=BodyAerodynamics.Evaluate(p,new DVector3(10,10,10),new DVector3(10,10,10));
            Assert.That(w.Force.Length+w.Torque.Length,Is.Zero);
        }
        [Test] public void LutInterpolationIsPositiveBoundedSymmetricAndExactAtSamples()
        {
            var p=Load("quad_test_projected_lut").Parameters;
            foreach(var s in p.AreaSamples) Assert.That(BodyAerodynamics.LookupArea(p.AreaSamples,s.Direction),Is.EqualTo(s.Area).Within(1e-12));
            var d=new DVector3(0.3,-0.7,0.9);
            double area=BodyAerodynamics.LookupArea(p.AreaSamples,d);
            Assert.That(area,Is.InRange(p.AreaSamples.Min(x=>x.Area),p.AreaSamples.Max(x=>x.Area)));
            Assert.That(BodyAerodynamics.LookupArea(p.AreaSamples,d*-1),Is.EqualTo(area).Within(1e-12));
            Assert.That(BodyAerodynamics.LookupArea(p.AreaSamples,d+new DVector3(1e-6,0,0)),Is.EqualTo(area).Within(1e-5));
        }
        [Test] public void SphereConstantLutNeedsNoDirectionalVariation()
        {
            var samples=new[]{new RuntimeAreaSample(new DVector3(0,1,0),Math.PI*0.25)};
            Assert.That(BodyAerodynamics.LookupArea(samples,new DVector3(2,1,-3)),Is.EqualTo(Math.PI*0.25).Within(1e-12));
        }
        [Test] public void RuntimeGeometrySnapshotDoesNotFollowDtoEdits()
        {
            var r=Load("quad_test_surfaces"); double area=r.Parameters.Surfaces[0].Area;
            r.Drone.bodyAerodynamics.surfaces[0].areaM2=999; r.Drone.bodyAerodynamics.surfaces[0].normalLocal[0]=999;
            Assert.That(r.Parameters.Surfaces[0].Area,Is.EqualTo(area)); Assert.That(r.Parameters.Surfaces[0].Normal.X,Is.Zero);
        }
        [TestCase("projected_box","dragCoefficient")] [TestCase("projected_box","projectedArea")]
        [TestCase("surfaces","surfaces")]
        public void MissingModelDataRejected(string mode,string field)
            => Assert.That(Load("quad_test_"+mode,j=>((JObject)j["bodyAerodynamics"]).Remove(field)).Success,Is.False);
        [Test] public void InvalidSurfaceNormalAndDuplicateIdRejected()
        {
            Assert.That(Load("quad_test_surfaces",j=>j["bodyAerodynamics"]["surfaces"][0]["normalLocal"]=new JArray(0,2,0)).Success,Is.False);
            Assert.That(Load("quad_test_surfaces",j=>j["bodyAerodynamics"]["surfaces"][1]["surfaceId"]=j["bodyAerodynamics"]["surfaces"][0]["surfaceId"].DeepClone()).Success,Is.False);
        }
        [Test] public void InvalidLutDirectionAndOppositeDuplicateRejected()
        {
            Assert.That(Load("quad_test_projected_lut",j=>j["bodyAerodynamics"]["projectedArea"]["samples"][0]["directionLocal"]=new JArray(0,0,0)).Success,Is.False);
            Assert.That(Load("quad_test_projected_lut",j=>
            {
                var samples=(JArray)j["bodyAerodynamics"]["projectedArea"]["samples"];
                var clone=(JObject)samples[0].DeepClone(); clone["directionLocal"]=new JArray(((JArray)clone["directionLocal"]).Values<double>().Select(x=>-x)); samples.Add(clone);
            }).Success,Is.False);
        }
        private static readonly int[] BoxTriangles={0,1,2,0,2,3,4,6,5,4,7,6,0,4,5,0,5,1,3,2,6,3,6,7,0,3,7,0,7,4,1,5,6,1,6,2};
        private static DVector3[] Box(DVector3 d) => new[]{new DVector3(0,0,0),new DVector3(d.X,0,0),new DVector3(d.X,d.Y,0),new DVector3(0,d.Y,0),
            new DVector3(0,0,d.Z),new DVector3(d.X,0,d.Z),d,new DVector3(0,d.Y,d.Z)};
        [TestCase(1,0,0)] [TestCase(0,1,0)] [TestCase(0,0,1)] [TestCase(1,1,1)] [TestCase(1,2,-3)]
        public void MeshBoxSilhouetteMatchesAnalyticProjection(double x,double y,double z)
        {
            var dimensions=new DVector3(0.4,0.1,0.4); var direction=new DVector3(x,y,z);
            double area=MeshSilhouette.Area(Box(dimensions),BoxTriangles,direction,128);
            double expected=BodyAerodynamics.BoxProjectedArea(dimensions,direction);
            Assert.That(area,Is.EqualTo(expected).Within(expected*0.025));
        }
        [Test] public void MeshScaleTranslationAndTriangleOverlapBehaveCorrectly()
        {
            var points=Box(new DVector3(1,1,1)); var direction=new DVector3(1,2,3);
            double area=MeshSilhouette.Area(points,BoxTriangles,direction,128);
            Assert.That(MeshSilhouette.Area(points.Select(x=>x*2+new DVector3(10,-4,2)).ToArray(),BoxTriangles,direction,128),Is.EqualTo(area*4).Within(1e-9));
            Assert.That(MeshSilhouette.Area(points,BoxTriangles.Concat(BoxTriangles).ToArray(),direction,128),Is.EqualTo(area).Within(1e-12));
            Assert.That(MeshSilhouette.Area(points,BoxTriangles,direction*-1,128),Is.EqualTo(area).Within(1e-12));
        }
        [Test] public void MeshSphereMatchesPiRadiusSquared()
        {
            var points=new List<DVector3>(); var indices=new List<int>(); const int latitude=32,longitude=64;
            for(int y=0;y<=latitude;y++) for(int x=0;x<longitude;x++)
            {
                double theta=Math.PI*y/latitude,phi=2*Math.PI*x/longitude;
                points.Add(new DVector3(Math.Sin(theta)*Math.Cos(phi),Math.Cos(theta),Math.Sin(theta)*Math.Sin(phi))*0.5);
            }
            for(int y=0;y<latitude;y++) for(int x=0;x<longitude;x++)
            {
                int a=y*longitude+x,b=y*longitude+(x+1)%longitude,c=a+longitude,d=b+longitude;
                indices.AddRange(new[]{a,b,c,b,d,c});
            }
            double area=MeshSilhouette.Area(points.ToArray(),indices.ToArray(),new DVector3(1,2,3),128);
            Assert.That(area,Is.EqualTo(Math.PI*0.25).Within(Math.PI*0.25*0.025));
        }
        [Test] public void UnreferencedMeshVerticesDoNotChangeSilhouetteOrHideInvalidIndices()
        {
            var box=Box(new DVector3(1,1,1)); var withUnused=box.Concat(new[]{new DVector3(100000,100000,100000)}).ToArray();
            Assert.That(MeshSilhouette.Area(withUnused,BoxTriangles,new DVector3(0,1,0),128),Is.EqualTo(1).Within(1e-12));
            Assert.Throws<ArgumentException>(()=>MeshSilhouette.Area(new[]{default(DVector3)},new[]{0,1,2},new DVector3(0,1,0),128));
        }
        [Test] public void BakeDirectionsCoverThirteenUniqueUnsignedAxes()
        {
            var directions=MeshSilhouette.BakeDirections(); Assert.That(directions.Length,Is.EqualTo(13));
            for(int i=0;i<directions.Length;i++)
            {
                Assert.That(directions[i].Length,Is.EqualTo(1).Within(1e-12));
                for(int j=0;j<i;j++) Assert.That(Math.Abs(DVector3.Dot(directions[i],directions[j])),Is.LessThan(0.999999));
            }
        }
    }
}
