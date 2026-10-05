using System;
using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace DroneLab.Physics.Tests
{
    public sealed class InertiaTensorTests
    {
        private static double[,] Tilted=>new double[,] { {.016,0,.002},{0,.026,0},{.002,0,.016} };
        private static string Read(string name) => ProfileTestFiles.Read(name);
        private static JObject ImportDocument()=>new JObject {
            ["schemaVersion"]="1.0.0",["coordinateConvention"]="UnityLeftHandedYUpZForward",["lengthUnit"]="m",["inertiaUnit"]="kg*m^2",
            ["tensorAbout"]="CenterOfMass",["massKg"]=1.2,["centerOfMassLocalM"]=new JArray(.01,0,-.02),
            ["matrixKgM2"]=new JArray(new JArray(.016,0,.002),new JArray(0,.026,0),new JArray(.002,0,.016)),["source"]="Synthetic test, not real CAD"
        };
        private static void Same(double[,] a,double[,] b,double tolerance)
        { for(int i=0;i<3;i++) for(int j=0;j<3;j++) Assert.That(b[i,j],Is.EqualTo(a[i,j]).Within(tolerance)); }
        [Test] public void RotatedTensorRecoversPrincipalMomentsAndFullOffDiagonalTerms()
        {
            var p=InertiaTensor.Diagonalize(Tilted);
            Assert.That(p.Moments.X,Is.EqualTo(.014).Within(1e-12)); Assert.That(p.Moments.Y,Is.EqualTo(.018).Within(1e-12)); Assert.That(p.Moments.Z,Is.EqualTo(.026).Within(1e-12));
            Same(Tilted,p.ToMatrixKgM2(),1e-12); var q=p.RotationXyzw; double norm=0; foreach(double x in q) norm+=x*x;
            Assert.That(norm,Is.EqualTo(1).Within(1e-14)); q[0]=999; Assert.That(p.RotationXyzw[0],Is.Not.EqualTo(999));
        }
        [TestCase(1e-9)] [TestCase(1)] [TestCase(1e9)]
        public void RandomPhysicalTensorsReconstructAtDifferentScales(double scale)
        {
            var random=new Random(827341);
            for(int sample=0;sample<200;sample++)
            {
                var tensor=new double[3,3];
                // Independent oracle: inertia of 20 point masses about their (computed) COM.
                var points=new double[20,3]; var mean=new double[3];
                for(int k=0;k<20;k++) for(int i=0;i<3;i++) { points[k,i]=random.NextDouble()*2-1; mean[i]+=points[k,i]/20; }
                for(int k=0;k<20;k++)
                {
                    double squared=0; for(int i=0;i<3;i++) { points[k,i]-=mean[i]; squared+=points[k,i]*points[k,i]; }
                    for(int i=0;i<3;i++) for(int j=0;j<3;j++) tensor[i,j]+=scale*((i==j ? squared : 0)-points[k,i]*points[k,j]);
                }
                Same(tensor,InertiaTensor.Diagonalize(tensor).ToMatrixKgM2(),scale*1e-10);
            }
        }
        [TestCase(1,1,1)] [TestCase(1,1,2)] [TestCase(2,1,1)] [TestCase(1,2,1)]
        public void RepeatedEigenvaluesAndPrincipalPermutationsAreValid(double x,double y,double z)
        {
            var matrix=new double[,] {{x,0,0},{0,y,0},{0,0,z}}; Same(matrix,InertiaTensor.Diagonalize(matrix).ToMatrixKgM2(),1e-12);
        }
        [Test] public void InvalidMatricesAreRejected()
        {
            Assert.Throws<ArgumentException>(()=>InertiaTensor.Diagonalize(null)); Assert.Throws<ArgumentException>(()=>InertiaTensor.Diagonalize(new double[2,3]));
            Assert.Throws<ArgumentException>(()=>InertiaTensor.Diagonalize(new double[3,3]));
            foreach(double bad in new[]{0d,-1d,double.NaN,double.PositiveInfinity})
                Assert.Throws<ArgumentException>(()=>InertiaTensor.Diagonalize(new double[,]{{bad,0,0},{0,1,0},{0,0,1}}));
            Assert.Throws<ArgumentException>(()=>InertiaTensor.Diagonalize(new double[,]{{1,.1,0},{0,1,0},{0,0,1}}));
            Assert.Throws<ArgumentException>(()=>InertiaTensor.Diagonalize(new double[,]{{1,0,0},{0,1,0},{0,0,3}}));
        }
        [Test] public void ImportPreservesRotorAeroBatteryAndValidatesAsExistingProfile()
        {
            var original=JObject.Parse(Read("quad_test_final_acceptance")); original["derived"]=new JObject { ["stale"]=true };
            var updated=JObject.Parse(CadInertiaImport.Apply(original.ToString(),ImportDocument().ToString()));
            foreach(string key in new[]{"rotors","bodyAerodynamics","powerSystem","physicsConfiguration","coordinateSystem"}) Assert.That(JToken.DeepEquals(original[key],updated[key]),Is.True,key);
            Assert.That(updated["derived"],Is.Null);
            var r=ProfileLoader.Load(updated.ToString(),Read("environment_final_acceptance"),Read("drone-profile.schema"),Read("environment-profile.schema"));
            Assert.That(r.Success,Is.True,string.Join("\n",r.Issues)); Assert.That(r.Parameters.Mass,Is.EqualTo(1.2)); Assert.That(r.Parameters.CenterOfMass.X,Is.EqualTo(.01));
        }
        [TestCase("coordinateConvention","CAD_Z_UP")] [TestCase("lengthUnit","mm")]
        [TestCase("inertiaUnit","g*mm^2")] [TestCase("tensorAbout","Origin")] [TestCase("schemaVersion","2.0.0")]
        public void ImplicitAxisUnitOrReferenceConversionIsRejected(string key,string value)
        { var d=ImportDocument(); d[key]=value; Assert.Throws<ArgumentException>(()=>CadInertiaImport.Apply(Read("quad_test_basic"),d.ToString())); }
        [Test] public void MissingUnknownNonNumericAndNonfiniteDataAreRejected()
        {
            foreach(string key in new[]{"source","matrixKgM2","centerOfMassLocalM","massKg"})
            { var d=ImportDocument(); d.Remove(key); Assert.Throws<ArgumentException>(()=>CadInertiaImport.Apply(Read("quad_test_basic"),d.ToString())); }
            var doc=ImportDocument(); doc["extra"]=1; Assert.Throws<ArgumentException>(()=>CadInertiaImport.Apply(Read("quad_test_basic"),doc.ToString()));
            foreach(JToken bad in new JToken[]{new JValue("1.2"),new JValue(-1),new JValue(double.NaN)})
            { doc=ImportDocument(); doc["massKg"]=bad; Assert.Throws<ArgumentException>(()=>CadInertiaImport.Apply(Read("quad_test_basic"),doc.ToString())); }
            doc=ImportDocument(); doc["matrixKgM2"]=new JArray(new JArray(1,2)); Assert.Throws<ArgumentException>(()=>CadInertiaImport.Apply(Read("quad_test_basic"),doc.ToString()));
        }
    }
}
